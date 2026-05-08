using System;
using UnityEngine;

namespace FTR.Gameplay.Common.Utils
{
    // TODO(optimization): This component is used to detect players in attack range,
    // It might be more efficient to use a non-physics based approach, like checking distances in the UseSystem tick.
    public class PlayerTriggerArea : MonoBehaviour
    {
        public event Action<Collider> OnPlayerEnter;
        public event Action<Collider> OnPlayerExit;

        private SphereCollider sphereCollider;

        public void Initialize(float radius)
        {
            SetRadius(radius);
        }

        public void SetRadius(float radius)
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
