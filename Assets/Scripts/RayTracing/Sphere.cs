using UnityEngine;

namespace RayTracing
{
    public class Sphere : MonoBehaviour
    {
        [SerializeField] private Color color;
        [SerializeField] private Color specular;
        [Range(0,1)]
        [SerializeField] private float smoothness;
        [SerializeField] private Color emission;

        public SphereInfo GetSphereInfo()
        {
            var sphere = new SphereInfo
            {
                position = transform.position,
                radius = transform.lossyScale[0] * 0.5f,
                albedo = new Vector3(color.r, color.g, color.b),
                specular = new Vector3(specular.r, specular.g, specular.b),
                smoothness = smoothness,
                emission = new Vector3(emission.r, emission.g, emission.b),
            };
            return sphere;
        }
    }
}