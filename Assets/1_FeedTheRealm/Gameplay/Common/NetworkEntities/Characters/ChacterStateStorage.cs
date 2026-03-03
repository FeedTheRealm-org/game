using System;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.Characters
{
    public class CharacterStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnPositionSync))]
        private Vector3 position;

        [SyncVar(hook = nameof(OnDirectionSync))]
        private Vector3 direction;

        /* --- Getters --- */

        public Vector3 Position => position;
        public Vector3 Direction => direction;

        public event Action<Vector3> OnPositionCorrected;
        public event Action<Vector3> OnDirectionChanged;

        /* --- Setters --- */

        [Server]
        public void SetDirection(Vector3 newDirection)
        {
            direction = newDirection;
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

        private void OnDirectionSync(Vector3 oldDirection, Vector3 newDirection)
        {
            OnDirectionChanged?.Invoke(newDirection);
        }
    }
}
