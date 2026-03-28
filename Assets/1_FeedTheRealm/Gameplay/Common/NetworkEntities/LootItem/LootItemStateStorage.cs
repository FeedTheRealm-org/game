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

        [SyncVar(hook = nameof(OnItemIdSync))]
        private string itemId;

        /* --- Callbacks --- */
        public event Action<Vector3> OnPositionCorrected;
        public event Action<string> OnItemIdChanged;

        public string ItemId => itemId;

        /* --- Setters --- */

        [Server]
        public void CorrectPosition(Vector3 newPosition)
        {
            position = newPosition;
        }

        [Server]
        public void SetItemId(string newItemId)
        {
            itemId = newItemId;
        }

        /* --- Syncvar hooks --- */

        private void OnPositionSync(Vector3 oldPosition, Vector3 newPosition)
        {
            OnPositionCorrected?.Invoke(newPosition);
        }

        private void OnItemIdSync(string oldId, string newId)
        {
            OnItemIdChanged?.Invoke(newId);
        }

        public override void OnStartClient()
        {
            Debug.Log(
                $"[LootItemStateStorage] Initial sync: position={position}, itemId={itemId}",
                this
            );
            OnPositionSync(Vector3.zero, position);
            OnItemIdSync(string.Empty, itemId);
        }
    }
}
