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
        [SerializeField]
        Logging.Logger logger;

        [Inject]
        private WorldMonitor worldMonitor;
        private string id;
        private bool isPickedUp = false;
        public bool IsPickedUp => isPickedUp;

        public void Initialize(uint netId)
        {
            id = $"LootItem-{netId}";
            logger.Log($"Initialized LootItemController with ID: {id}", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            logger.Log($"{other.gameObject.name} entered trigger of {gameObject.name} (ID: {id}).");
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
                logger.Log($"{gameObject.name} (ID: {id}) picked up successfully, despawning...");
            Destroy(gameObject);
        }

        private void SendPickupCommand(uint playerId)
        {
            PickUpCommand command = new(playerId, id, AfterPickup);
            worldMonitor.Commands.Enqueue(command);
        }

        private void OnDrawGizmos()
        {
            var radius = GetComponentInParent<SphereCollider>().radius;
            Gizmos.color = Color.chocolate;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
