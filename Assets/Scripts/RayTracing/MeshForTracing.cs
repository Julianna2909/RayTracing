using UnityEngine;

namespace RayTracing
{
    public class MeshForTracing : BaseTracingObject
    {
        [SerializeField] private MeshFilter mashFilter;

        public MeshFilter MashFilter => mashFilter;

        public void GetMaterialParameters(out Vector3 albedo, out Vector3 specular, out float smoothness, out Vector3 emission)
        {
            albedo = new Vector3(color.r, color.g, color.b);
            specular = new Vector3(this.specular.r, this.specular.g, this.specular.b);
            smoothness = this.smoothness;
            emission = new Vector3(this.emission.r, this.emission.g, this.emission.b);
        }
    }
}