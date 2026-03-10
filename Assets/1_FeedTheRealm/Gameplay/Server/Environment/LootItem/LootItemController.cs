using FTR.Core.Server.Commands;
using FTR.Core.Server.EventChannels;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Environment.LootItem
{
    /// <summary>
    /// LootItem represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class LootItemController : MonoBehaviour
    {
        [Inject]
        private WorldMonitor worldMonitor;
        private string id;
        private bool isPickedUp = false;
        public bool IsPickedUp => isPickedUp;

        public void Initialize(uint netId)
        {
            id = $"LootItem-{netId}";
        }

        // In the Physics2D settings,
        // The collision Player only collides with the LootItem layer,
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
                Debug.Log($"Item {id} picked up successfully, despawning...");
            Destroy(gameObject);
        }

        private void SendPickupCommand(uint playerId)
        {
            PickUpCommand command = new(playerId, id, AfterPickup);
            worldMonitor.Commands.Enqueue(command);
        }
    }
}
