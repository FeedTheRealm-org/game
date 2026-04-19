using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Registry;
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
                // TODO: we have to validate the last position of the player.
                // The main issue is that if we teleport between zones, we need to load the last position saved
                // in the world DB to validate the position.
                // For now we wont validate the last position before TP but that logic can be added here

                if (portalRegistry.TryGetPortal(portalInfo.DestinationId, out var targetInfo))
                {
                    movementSystem.LoadPosition(targetInfo.Position);
                }
                else
                {
                    Debug.LogError(
                        $"[TeleportSystem] No destination portal found with id {portalInfo.DestinationId} | Cannot teleport player."
                    );
                    return;
                }
                FinishTeleport();
            }
            else
                Debug.LogError(
                    $"[TeleportSystem] No portal found with id {portalId} | Cannot teleport player."
                );
        }

        private void FinishTeleport()
        {
            worldMonitor.Events.Enqueue(
                new OpenPortalEvent(
                    netId,
                    new OpenPortalEventContent { PortalId = "", DestinationName = "" },
                    GetPlayerConnectionId(netId).Value
                )
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
