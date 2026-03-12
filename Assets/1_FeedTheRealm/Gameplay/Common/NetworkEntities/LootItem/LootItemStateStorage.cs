using System;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.LootItem
{
    public class LootItemStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnPositionSync))]
        private Vector3 position;

        /* --- Getters --- */

        public Vector3 Position => position;
        public bool IsGrounded { get; set; }
        public bool IsMovementBlocked { get; set; }

        public event Action<Vector3> OnPositionCorrected;
        public event Action<Vector3> OnDirectionChanged;
        public event Action<float> OnStaminaChanged;
        public event Action<float> OnHealthChanged;

        /* --- Setters --- */

        [Server]
        public void CorrectPosition(Vector3 newPosition)
        {
            position = newPosition;
        }

        /* --- Syncvar hooks --- */

        private void OnPositionSync(Vector3 oldPosition, Vector3 newPosition)
        {
            OnPositionCorrected?.Invoke(newPosition);
        }

        private void OnInitialForceSync(Vector3 oldInitialForce, Vector3 newInitialForce)
        {
            OnPositionCorrected?.Invoke(newInitialForce);
        }

        public override void OnStartClient()
        {
            Debug.Log($"[LootStateStorage] Initial sync: position={position}", this);
            OnPositionSync(Vector3.zero, position);
        }
    }
}
