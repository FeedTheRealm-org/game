using System;
using UnityEngine;

namespace FTR.Gameplay.Common.Utils
{
    public class PlayerTriggerArea : MonoBehaviour
    {
        public event Action<Collider> OnPlayerEnter;
        public event Action<Collider> OnPlayerExit;

        public void Initialize(float radius)
        {
            SphereCollider collider = gameObject.GetComponent<SphereCollider>();
            collider.radius = radius;
        }

        private void OnTriggerEnter(Collider other)
        {
            OnPlayerEnter?.Invoke(other);
        }

        private void OnTriggerExit(Collider other)
        {
            OnPlayerExit?.Invoke(other);
        }
    }
}
