using UnityEngine;

namespace RayTracing
{
    public class BaseTracingObject : MonoBehaviour
    {
        [SerializeField] protected Color color;
        [SerializeField] protected Color specular;
        [Range(0,1)] [SerializeField] protected float smoothness;
        [SerializeField] protected Color emission;
    }
}