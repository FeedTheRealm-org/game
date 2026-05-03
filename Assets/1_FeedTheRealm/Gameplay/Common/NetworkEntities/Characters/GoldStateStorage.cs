using System;
using Mirror;

namespace FTR.Gameplay.Common.NetworkEntities.Gold
{
    public class GoldStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnGoldSync))]
        private int gold = 0;

        /* --- Getters --- */

        public int Gold => gold;

        /* --- Events --- */

        public event Action<int> OnGoldChanged;

        /* --- Setters --- */

        [Server]
        public void SetGold(int amount)
        {
            gold = amount;
        }

        [Server]
        public void AddGold(int amount)
        {
            gold += amount;
        }

        [Server]
        public void ReduceGold(int amount)
        {
            if (amount > gold)
                return;
            gold -= amount;
        }

        /* --- SyncVar hooks --- */

        private void OnGoldSync(int oldGold, int newGold)
        {
            OnGoldChanged?.Invoke(newGold);
        }
    }
}
