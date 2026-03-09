using System.Collections;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Environment.Items
{
    /// <summary>
    /// LootItem represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class LootItemController : MonoBehaviour
    {
        private string id;

        [Inject]
        private readonly WorldMonitor worldMonitor;

        [Inject]
        private readonly ServerConfig config;

        private bool isPickedUp = false;

        private uint despawnTime = 120;

        public string Id => id;

        private void OnEnable()
        {
            id = GetComponent<NetworkIdentity>().netId.ToString();
            despawnTime = config.ItemDespawnTime > 0 ? config.ItemDespawnTime : despawnTime;
            StartCoroutine(DespawnObject());
        }

        private IEnumerator DespawnObject()
        {
            yield return new WaitForSeconds(despawnTime);
            if (!isPickedUp)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isPickedUp)
                return;
            uint playerId = other.gameObject.GetComponent<NetworkIdentity>().netId;
            SendPickupCommand(playerId);
            isPickedUp = true;
        }

        private void AfterPickup(bool success)
        {
            if (!success)
                isPickedUp = false;
            else
                Destroy(gameObject);
        }

        private void SendPickupCommand(uint playerId)
        {
            PickUpCommand command = new(playerId, id, AfterPickup);
            worldMonitor.Commands.Enqueue(command);
            Debug.Log($"HELLO I GOT PICKED UP!!! {id} by player {playerId}");
        }
    }
}
