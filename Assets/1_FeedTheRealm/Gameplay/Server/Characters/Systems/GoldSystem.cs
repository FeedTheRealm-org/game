using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Gold;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class GoldSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ServerConfig config;

        private uint netId;
        private GoldStateStorage goldState;

        private bool isInitialized = false;
        private bool isSettedUp = false;

        public void Initialize(uint netId, GoldStateStorage goldState)
        {
            this.netId = netId;
            this.goldState = goldState;
            isInitialized = true;
        }

        public void AddGold(IEventCollectable ec, int amount)
        {
            if (amount <= 0)
            {
                logger.Log(
                    $"[GoldSystem] AddGold: invalid amount {amount} for player {netId}",
                    this
                );
                return;
            }

            logger.Log($"[GoldSystem] Player {netId} gained {amount} gold", this);
            goldState.AddGold(amount);
        }

        public bool ReduceGold(IEventCollectable ec, int amount)
        {
            if (amount <= 0)
            {
                logger.Log(
                    $"[GoldSystem] ReduceGold: invalid amount {amount} for player {netId}",
                    this
                );
                return false;
            }

            goldState.ReduceGold(amount);
            logger.Log($"[GoldSystem] Player {netId} reduced {amount} gold", this);
            return true;
        }

        public void LoadGold(int savedGold)
        {
            // TODO: restore gold from saved data
            logger.Log($"[GoldSystem] Loaded gold for player {netId}", this);
        }

        public void GameTick(float dt)
        {
            if (isInitialized && !isSettedUp)
            {
                int currentGold = config.StartingGold > 0 ? config.StartingGold : 0;

                goldState.SetGold(currentGold);
                logger.Log($"[GoldSystem] Initialized player {netId} with {currentGold}", this);
                isSettedUp = true;
            }
        }
    }
}
