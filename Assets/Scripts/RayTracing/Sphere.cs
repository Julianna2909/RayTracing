using UnityEngine;

namespace RayTracing
{
    public class Sphere : MonoBehaviour
    {
        [SerializeField] private Color color;
        [SerializeField] private Vector3 specular;
        [SerializeField] private float smoothness;
        [SerializeField] private Vector3 emission;

        public SphereInfo GetSphereInfo()
        {
            var sphere = new SphereInfo
            {
                position = transform.position,
                radius = transform.lossyScale[0] * 0.5f,
                albedo = new Vector3(color.r, color.g, color.b),
                specular = specular,
                smoothness = smoothness, emission = emission
            };
            return sphere;
        }
    }
}