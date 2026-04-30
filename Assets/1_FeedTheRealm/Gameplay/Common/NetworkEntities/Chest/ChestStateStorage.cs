using System;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.Chest
{
    public class ChestStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnChestSync))]
        private bool isOpen = false;

        [SyncVar(hook = nameof(OnChestDataSync))]
        private ChestData chestData;

        private void OnChestSync(bool oldState, bool newState)
        {
            OnChestStateChanged?.Invoke(newState);
        }

        private void OnChestDataSync(ChestData oldChestData, ChestData newChestData)
        {
            OnChestDataInitialized?.Invoke(newChestData);
        }

        public event Action<bool> OnChestStateChanged;
        public event Action<ChestData> OnChestDataInitialized;

        public bool IsOpen => isOpen;
        public ChestData ChestData => chestData;

        [Server]
        public void SetChestState(bool isOpen)
        {
            Debug.Log($"[Server] Setting chest state to: {isOpen}");
            this.isOpen = isOpen;
        }

        public void SetChestData(ChestData chestData)
        {
            this.chestData = chestData;
        }

        public override void OnStartClient()
        {
            OnChestSync(false, isOpen);
            OnChestDataSync(null, chestData);
        }
    }
}
