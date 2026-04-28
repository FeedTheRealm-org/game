using System;
using Mirror;

namespace FTR.Gameplay.Common.NetworkEntities.Chest
{
    public class ChestStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnChestSync))]
        private bool isOpen = false;

        private void OnChestSync(bool oldState, bool newState)
        {
            OnChestStateChanged?.Invoke(newState);
        }

        public event Action<bool> OnChestStateChanged;

        public bool IsOpen => isOpen;

        [Server]
        public void SetChestState(bool isOpen)
        {
            this.isOpen = isOpen;
        }
    }
}
