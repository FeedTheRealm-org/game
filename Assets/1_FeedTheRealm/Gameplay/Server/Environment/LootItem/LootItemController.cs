using System;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Config;
using FTR.Gameplay.Server.Reaper;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Environment.LootItem
{
    /// <summary>
    /// LootItem represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class LootItemController : MonoBehaviour, IReapable
    {
        [SerializeField]
        Logging.Logger logger;

        [SerializeField]
        private ServerConfig config;

        [Inject]
        private WorldMonitor worldMonitor;
        private string itemId;
        private int goldAmount;
        private bool isPickedUp = false;
        public bool IsPickedUp => isPickedUp;

        public void Initialize(uint netId, string actualItemId, int goldAmount)
        {
            itemId = $"LootItem-{netId}";
            this.itemId = actualItemId;
            this.goldAmount = goldAmount;
            logger.Log(
                $"Initialized LootItemController with ID: {itemId}, ItemID: {itemId}, GoldAmount: {goldAmount}",
                this
            );
        }

        private void OnTriggerEnter(Collider other)
        {
            Gizmos.color = Color.green;

            logger.Log(
                $"{other.gameObject.name} entered trigger of {gameObject.name} (ID: {itemId}, ItemID: {itemId})."
            );
            if (isPickedUp)
            {
                return;
            }

            var networkIdentity = other.gameObject.GetComponentInParent<NetworkIdentity>();
            if (networkIdentity == null)
            {
                logger.Log(
                    $"[LootItemController] {other.gameObject.name} does not have a NetworkIdentity.",
                    this
                );
                return;
            }

            uint playerId = networkIdentity.netId;
            logger.Log($"[LootItemController] Target PlayerId for PickUpCommand: {playerId}", this);
            SendPickupCommand(playerId);
            isPickedUp = true;
        }

        private void OnTriggerExit(Collider other)
        {
            Gizmos.color = Color.chocolate;
        }

        private void SendPickupCommand(uint playerId)
        {
            PickUpCommand command = new(playerId, itemId, goldAmount, AfterPickup);
            worldMonitor.Commands.Enqueue(command);
        }

        private void AfterPickup(bool success)
        {
            logger.Log(
                $"AfterPickup callback for {gameObject.name} (ID: {itemId}), success: {success}, isPickedUp: {isPickedUp}"
            );
            if (!success)
                isPickedUp = false;
            else
                Despawn();
        }

        private void Despawn()
        {
            logger.Log(
                $"{gameObject.name} (ID: {itemId}) picked up status: {isPickedUp}, despawning..."
            );
            NetworkServer.Destroy(transform.parent.gameObject);
        }

        // ----- IReapable Implementation -----

        public bool CanReap()
        {
            return true;
        }

        public DateTime SpawnTime { get; set; }
        public float MinimumLifetimeSeconds => config.ItemDespawnTime;

        // ----- Debugging -----

        private void OnDrawGizmos()
        {
            var radius = GetComponentInParent<SphereCollider>().radius;
            Gizmos.color = Color.chocolate;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
