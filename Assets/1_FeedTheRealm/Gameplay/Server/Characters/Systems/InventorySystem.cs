using System;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Commands;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Environment.Items
{
    /// <summary>
    /// LootItem represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class InventorySystem : MonoBehaviour, IGameTickable
    {
        public void GameTick(float dt) { }

        internal void OnPickUp(IEventCollectable ec, string itemId, Action<bool> onComplete)
        {
            var playerId = GetComponent<NetworkIdentity>().netId;
            Debug.Log($"Attempting to pick up item {itemId} for player {playerId}");
            onComplete(true);
        }
    }
}
