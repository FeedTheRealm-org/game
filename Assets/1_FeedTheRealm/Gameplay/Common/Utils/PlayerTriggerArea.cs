using System;
using UnityEngine;

namespace FTR.Gameplay.Common.Utils
{
    public class PlayerTriggerArea : MonoBehaviour
    {
        public event Action<Collider> OnPlayerEnter;
        public event Action<Collider> OnPlayerExit;

        private SphereCollider sphereCollider;

        public void Initialize(float radius)
        {
            sphereCollider = gameObject.GetComponent<SphereCollider>();
            sphereCollider.radius = radius;
        }

        private void OnTriggerEnter(Collider other)
        {
            OnPlayerEnter?.Invoke(other);
        }

        private void OnTriggerExit(Collider other)
        {
            OnPlayerExit?.Invoke(other);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 0, 0.1f);
            Gizmos.DrawSphere(transform.position, gameObject.GetComponent<SphereCollider>().radius);
        }
    }
}
