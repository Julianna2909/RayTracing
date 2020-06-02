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

        private RenderTexture converged;
        private ComputeBuffer sphereBuffer;
        private RenderTexture target;
        private uint currentSample;
        private Material addMaterial;
        private float lastFieldOfView;
        private List<Transform> transformsToWatch = new List<Transform>();

        private void Awake()
        {
            transformsToWatch.Add(transform);
            transformsToWatch.Add(directionalLight.transform);
        }

        private void OnEnable()
        {
            InitSpheresBuffer();
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
        
        private void OnDisable()
        {
            sphereBuffer?.Release();
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
}
