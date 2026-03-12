using System;
using FTR.Core.Common.Systems.Status;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.LootItem
{
    public class LootItemStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnPositionSync))]
        private Vector3 position;

        /* --- Callbacks --- */
        public event Action<Vector3> OnPositionCorrected;

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

        public override void OnStartClient()
        {
            Debug.Log($"[LootItemStateStorage] Initial sync: position={position}", this);
            OnPositionSync(Vector3.zero, position);
        }
    }
}
