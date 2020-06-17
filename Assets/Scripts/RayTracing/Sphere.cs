using UnityEngine;

namespace RayTracing
{
    public class Sphere : BaseTracingObject
    {
        public SphereInfo GetSphereInfo()
        {
            var sphere = new SphereInfo
            {
                Position = transform.position,
                Radius = transform.lossyScale[0] * 0.5f,
                Albedo = new Vector3(color.r, color.g, color.b),
                Specular = new Vector3(specular.r, specular.g, specular.b),
                Smoothness = smoothness,
                Emission = new Vector3(emission.r, emission.g, emission.b),
            };
            return sphere;
        }
    }
}