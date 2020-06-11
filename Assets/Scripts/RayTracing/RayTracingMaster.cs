using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using Random = UnityEngine.Random;

namespace RayTracing
{
    public class RayTracingMaster : MonoBehaviour
    {
        [SerializeField] private ComputeShader rayTracingShader;
        [SerializeField] private Camera renderCamera;
        [SerializeField] private Texture skyboxTexture;
        [SerializeField] private Light directionalLight;
        [SerializeField] private List<Sphere> spheres;
        [SerializeField] private List<GameObject> objectsToTrace;

        private RenderTexture converged;
        private ComputeBuffer sphereBuffer;
        private RenderTexture target;
        private uint currentSample;
        private Material addMaterial;
        private float lastFieldOfView;
        private List<Transform> transformsToWatch = new List<Transform>();
        
        // objects buffer //
        private List<MeshInfo> meshesInfo;
        private List<Vector3> vertices;
        private List<int> indices;
        private ComputeBuffer meshObjectBuffer;
        private ComputeBuffer vertexBuffer;
        private ComputeBuffer indexBuffer;

        private void Awake()
        {
            transformsToWatch.Add(transform);
            transformsToWatch.Add(directionalLight.transform);
        }

        private void OnEnable()
        {
            vertices = new List<Vector3>();
            indices = new List<int>();
            meshesInfo = new List<MeshInfo>();
            InitSpheresBuffer();
            RebuildMeshObjectBuffers();
            currentSample = 0;
        }

        private void InitSpheresBuffer()
        {
            sphereBuffer?.Release();
            var spheresInfo = new List<SphereInfo>();
            spheres.ForEach(s =>
            {
                spheresInfo.Add(s.GetSphereInfo());
//                s.gameObject.SetActive(false);
            });
            sphereBuffer = new ComputeBuffer(spheres.Count, 56);
            sphereBuffer.SetData(spheresInfo);
        }
        
        private void RebuildMeshObjectBuffers()
        {
            if (objectsToTrace.Count == 0) return;
            currentSample = 0;

            // Clear all lists
            vertices.Clear();
            indices.Clear();

            // Loop over all objects and gather their data
            objectsToTrace.ForEach(o =>
            {
                var mesh = o.GetComponent<MeshFilter>().sharedMesh;
                var firstVertex = vertices.Count;
                vertices.AddRange(mesh.vertices);
                
                var firstIndex = indices.Count;
                var objectIndices = mesh.GetIndices(0);
                indices.AddRange(objectIndices.Select(index => index + firstVertex));
                
                meshesInfo.Add(new MeshInfo
                {
                    localToWorldMatrix = o.transform.localToWorldMatrix,
                    indicesOffset = firstIndex,
                    indicesCount = objectIndices.Length
                });
            });

            CreateComputeBuffer(ref meshObjectBuffer, meshesInfo, 72);
            CreateComputeBuffer(ref vertexBuffer, vertices, 12);
            CreateComputeBuffer(ref indexBuffer, indices, 4);
        }

        private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
            where T : struct
        {
            if (buffer != null)
            {
                if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
                {
                    buffer.Release();
                    buffer = null;
                }
            }

            if (data.Count != 0)
            {
                if (buffer == null)
                {
                    buffer = new ComputeBuffer(data.Count, stride);
                }
                buffer.SetData(data);
            }
        }

        private void SetComputeBuffer(string name, ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                rayTracingShader.SetBuffer(0, name, buffer);
            }
        }

        private void SetShaderParameters()
        {
            rayTracingShader.SetVector("PixelOffset", new Vector2(Random.value, Random.value));
            rayTracingShader.SetMatrix("CameraToWorld", renderCamera.cameraToWorldMatrix);
            rayTracingShader.SetMatrix("CameraInverseProjection", renderCamera.projectionMatrix.inverse);
            rayTracingShader.SetTexture(0, "SkyboxTexture", skyboxTexture);
            rayTracingShader.SetFloat("Seed", Random.value);
            var lightForward = directionalLight.transform.forward;
            rayTracingShader.SetVector("DirectionalLight",
                new Vector4(lightForward.x, lightForward.y, lightForward.z, directionalLight.intensity));
            if (spheres != null)
                rayTracingShader.SetBuffer(0, "Spheres", sphereBuffer);
            if (meshObjectBuffer == null) return;
            SetComputeBuffer("MeshObjects", meshObjectBuffer);
            SetComputeBuffer("Vertices", vertexBuffer);
            SetComputeBuffer("Indices", indexBuffer);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            SetShaderParameters();
            Render(destination);
        }

        private void Render(RenderTexture destination)
        {
            InitRenderTexture();
            rayTracingShader.SetTexture(0, "Result", target);
            var threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
            var threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
            rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
            
            if (addMaterial == null)
                addMaterial = new Material(Shader.Find("Hidden/AddShader"));
            addMaterial.SetFloat("_Sample", currentSample);
            Graphics.Blit(target, converged, addMaterial);
            Graphics.Blit(converged, destination);
            currentSample++;
        }

        private void InitRenderTexture()
        {
            if (target == null || target.width != Screen.width || target.height != Screen.height)
            {
                // Release render texture if we already have one
                if (target != null)
                {
                    target.Release();
                    converged.Release();
                }

                // Get a render target for Ray Tracing
                target = new RenderTexture(Screen.width, Screen.height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) {enableRandomWrite = true};
                target.Create();
                converged = new RenderTexture(Screen.width, Screen.height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) {enableRandomWrite = true};
                converged.Create();
                currentSample = 0;
            }
        }

        private void Update()
        {
            
            if (Math.Abs(renderCamera.fieldOfView - lastFieldOfView) > 0.000001f)
            {
                currentSample = 0;
                lastFieldOfView = renderCamera.fieldOfView;
            }

            foreach (Transform t in transformsToWatch)
            {
                if (t.hasChanged)
                {
                    currentSample = 0;
                    t.hasChanged = false;
                }
            }
        }
        private void OnDisable()
        {
            sphereBuffer?.Release();
        }
    }

    public struct SphereInfo
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public Vector3 emission;
    }

    public struct MeshInfo
    {
        public Matrix4x4 localToWorldMatrix;
        public int indicesOffset;
        public int indicesCount;
    }
}
