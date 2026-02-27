using System;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.Characters
{
    public class CharacterStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnPositionSync))]
        private Vector3 position;

        [SyncVar(hook = nameof(OnVelocitySync))]
        private Vector3 velocity;

        /* --- Getters --- */

        public Vector3 Position => position;
        public Vector3 Velocity => velocity;

        public event Action<Vector3> OnPositionCorrected;
        public event Action<Vector3> OnVelocityChanged;

        /* --- Setters --- */

        [Server]
        public void SetVelocity(Vector3 newVelocity)
        {
            velocity = newVelocity;
        }

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

        private void OnVelocitySync(Vector3 oldVelocity, Vector3 newVelocity)
        {
            OnVelocityChanged?.Invoke(newVelocity);
        }
    }
}
