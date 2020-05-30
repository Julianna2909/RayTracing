using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

        private ComputeBuffer sphereBuffer;
        private RenderTexture target;
        private uint currentSample;
        private Material addMaterial;
        private bool isSceneGenerated;

        private void OnEnable()
        {
            InitSpheresBuffer();
            currentSample = 0;
        }

        private void InitSpheresBuffer()
        {
            var spheresInfo = new List<SphereInfo>();
            spheres.ForEach(s =>
            {
                spheresInfo.Add(s.GetSphereInfo());
                s.gameObject.SetActive(false);
            });
            sphereBuffer = new ComputeBuffer(spheres.Count, 40);
            sphereBuffer.SetData(spheresInfo);
            isSceneGenerated = true;
        }

        private void SetShaderParameters()
        {
            rayTracingShader.SetVector("PixelOffset", new Vector2(Random.value, Random.value));
            rayTracingShader.SetMatrix("CameraToWorld", renderCamera.cameraToWorldMatrix);
            rayTracingShader.SetMatrix("CameraInverseProjection", renderCamera.projectionMatrix.inverse);
            rayTracingShader.SetTexture(0, "SkyboxTexture", skyboxTexture);
            var lightForward = directionalLight.transform.forward;
            rayTracingShader.SetVector("DirectionalLight",
                new Vector4(lightForward.x, lightForward.y, lightForward.z, directionalLight.intensity));
            rayTracingShader.SetBuffer(0, "Spheres", sphereBuffer);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!isSceneGenerated) return; 
            SetShaderParameters();
            Render(destination);
        }

        private void Render(RenderTexture destination)
        {
            // Make sure we have a current render target
            InitRenderTexture();

            // Set the target and dispatch the compute shader
            rayTracingShader.SetTexture(0, "Result", target);
            int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
            rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
            
            // Blit the result texture to the screen
            if (addMaterial == null)
                addMaterial = new Material(Shader.Find("Hidden/AddShader"));
            addMaterial.SetFloat("Sample", currentSample);
            Graphics.Blit(target, destination, addMaterial);
            currentSample++;
        }

        private void InitRenderTexture()
        {
            if (target == null || target.width != Screen.width || target.height != Screen.height)
            {
                // Release render texture if we already have one
                if (target != null)
                    target.Release();

                // Get a render target for Ray Tracing
                target = new RenderTexture(Screen.width, Screen.height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) {enableRandomWrite = true};
                target.Create();
            }
        }
        
        private void OnDisable()
        {
            isSceneGenerated = false;
            sphereBuffer?.Release();
        }

        private void Update()
        {
            if (!transform.hasChanged) return;
            currentSample = 0;
            transform.hasChanged = false;
        }
    }

    public struct SphereInfo
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    }
}
