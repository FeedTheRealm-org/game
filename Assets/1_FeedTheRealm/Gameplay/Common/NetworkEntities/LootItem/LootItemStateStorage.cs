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

        [SyncVar(hook = nameof(OnGoldAmountSync))]
        private int goldAmount;

        /* --- Callbacks --- */
        public event Action<Vector3> OnPositionCorrected;
        public event Action<string> OnItemIdChanged;
        public event Action<int> OnGoldAmountChanged;

        public string ItemId => itemId;
        public int GoldAmount => goldAmount;

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

        [Server]
        public void SetGoldAmount(int amount)
        {
            goldAmount = amount;
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

        private void OnGoldAmountSync(int oldAmount, int newAmount)
        {
            OnGoldAmountChanged?.Invoke(newAmount);
        }

        public override void OnStartClient()
        {
            Debug.Log(
                $"[LootItemStateStorage] Initial sync: position={position}, itemId={itemId}, goldAmount={goldAmount}",
                this
            );
            OnPositionSync(Vector3.zero, position);
            OnItemIdSync(string.Empty, itemId);
            OnGoldAmountSync(0, goldAmount);
        }
    }
}
