using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Environment.Portal;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class TeleportSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private PortalRegistry portalRegistry;

        private MovementSystem movementSystem;
        private CharacterStateStorage stateStorage;
        private uint netId;
        private WorldMonitor worldMonitor;

        public void Initialize(
            MovementSystem movementSystem,
            CharacterStateStorage stateStorage,
            WorldMonitor worldMonitor,
            uint netId
        )
        {
            this.movementSystem = movementSystem;
            this.stateStorage = stateStorage;
            this.worldMonitor = worldMonitor;
            this.netId = netId;
        }

        public void GameTick(float dt) { }

        public void OnTeleport(string portalId)
        {
            if (portalRegistry.TryGetPortal(portalId, out var portalInfo))
            {
                if (
                    Vector3.Distance(stateStorage.Position, portalInfo.PlacementData.position)
                    <= portalInfo.PlacementData.radius
                )
                {
                    movementSystem.LoadPosition(portalInfo.Destination);

                    // We send an empty message to notify to stop displaying the teleportation animation and hide the loading screen.
                    // The actual teleportation is handled by the movement system, so we don't need to send the new position here.
                    worldMonitor.Events.Enqueue(
                        new OpenPortalEvent(
                            netId,
                            new OpenPortalEventContent { PortalId = "", DestinationName = "" },
                            GetPlayerConnectionId(netId).Value
                        )
                    );
                }
                else
                    Debug.LogError(
                        $"[TeleportSystem] Player is not within portal radius for {portalId}. | Cannot teleport player."
                    );
            }
            else
                Debug.LogError(
                    $"[TeleportSystem] No portal found with id {portalId} | Cannot teleport player."
                );
        }

        private int? GetPlayerConnectionId(uint playerNetId)
        {
            if (
                worldMonitor.Entities.TryGet(playerNetId, out var entity)
                && entity.ConnectionId.HasValue
            )
                return entity.ConnectionId.Value;

            return null;
        }
    }
}
