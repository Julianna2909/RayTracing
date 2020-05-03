using UnityEngine;

namespace RayTracing
{
    public class RayTracingMaster : MonoBehaviour
    {
        [SerializeField] private ComputeShader rayTracingShader;
        [SerializeField] private Camera renderCamera;
        [SerializeField] private Texture skyboxTexture;

        private RenderTexture target;
        private uint currentSample = 0;
        private Material addMaterial;

        private void SetShaderParameters()
        {
            rayTracingShader.SetVector("PixelOffset", new Vector2(Random.value, Random.value));
            rayTracingShader.SetMatrix("CameraToWorld", renderCamera.cameraToWorldMatrix);
            rayTracingShader.SetMatrix("CameraInverseProjection", renderCamera.projectionMatrix.inverse);
            rayTracingShader.SetTexture(0, "SkyboxTexture", skyboxTexture);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
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
            addMaterial.SetFloat("_Sample", currentSample);
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
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                target.enableRandomWrite = true;
                target.Create();
            }
        }
        
        private void Update()
        {
            if (transform.hasChanged)
            {
                currentSample = 0;
                transform.hasChanged = false;
            }
        }
    }
}
