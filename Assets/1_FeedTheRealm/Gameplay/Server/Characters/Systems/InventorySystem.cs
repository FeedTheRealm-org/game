using System;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Events;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// LootItem represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class InventorySystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private Logging.Logger logger;
        private uint netId;

        public void Initialize(uint netId)
        {
            this.netId = netId;
            logger.Log($"Initializing InventorySystem for player {netId}", this);
        }

        internal void OnPickUp(IEventCollectable ec, string itemId, Action<bool> onComplete)
        {
            logger.Log($"Attempting to pick up item {itemId} for player {netId}", this);
            onComplete(true);
        }

        public void GameTick(float dt) { }
    }
}
