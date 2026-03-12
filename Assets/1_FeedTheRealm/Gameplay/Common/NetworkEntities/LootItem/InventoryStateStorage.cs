using System;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.LootItem
{
    public struct LastItemData
    {
        public string itemId;
        public byte itemPosition;

        public LastItemData(string itemId, byte itemPosition)
        {
            this.itemId = itemId;
            this.itemPosition = itemPosition;
        }
    }

    public class InventoryStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnLastItemSync))]
        private LastItemData lastItemData;

        /* --- Getters --- */

        public LastItemData LastItem => lastItemData;

        public event Action<LastItemData> OnLastItemChanged;

        /* --- Setters --- */

        [Server]
        public void AddItem(string itemId, byte position)
        {
            lastItemData = new LastItemData(itemId, position);
        }

        private void OnLastItemSync(LastItemData oldLastItemData, LastItemData newLastItemData)
        {
            OnLastItemChanged?.Invoke(newLastItemData);
        }
    }
}
