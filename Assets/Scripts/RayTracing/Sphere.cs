using UnityEngine;

namespace RayTracing
{
    public class Sphere : MonoBehaviour
    {
        [SerializeField] private Color color;
        [SerializeField] private Vector3 specular;

        public SphereInfo GetSphereInfo()
        {
            var sphere = new SphereInfo
            {
                position = transform.position,
                radius = transform.lossyScale[0] * 0.5f,
                albedo = new Vector3(color.r, color.g, color.b),
                specular = specular
            };
            return sphere;
        }
    }
}