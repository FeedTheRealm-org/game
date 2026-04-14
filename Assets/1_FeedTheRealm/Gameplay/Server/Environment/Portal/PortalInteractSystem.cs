using FTR.Core.Common.Interactions;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Portal;
using FTR.Gameplay.Server.Environment.Quest;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Environment.Portal
{
    /// <summary>
    /// Server-side teleport system. Handles teleportation logic and validation.
    /// </summary>
    public class PortalInteractSystem : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private PortalRegistry portalRegistry;

        private PortalStateStorage portalStateStorage;

        private PortalData portalData = null;
        private WorldMonitor worldMonitor;

        public bool CanInteract(IInteractor interactor)
        {
            return true;
        }

        public void ContinueInteraction(IInteractor interactor)
        {
            return;
        }

        public void StopInteraction(IInteractor interactor)
        {
            return;
        }

        public void Initialize(string portalId)
        {
            if (portalRegistry.TryGetPortal(portalId, out var portalInfo))
            {
                portalData = portalInfo.Data;
                logger.Log($"[PortalInteractSystem] Portal ID set to {portalId}.", this);
            }
            else
            {
                logger.Log(
                    $"[PortalInteractSystem] Failed to set Portal ID. No portal found with ID: {portalId}.",
                    this
                );
            }
        }

        public void Initialize(WorldMonitor worldMonitor, PortalStateStorage portalStateStorage)
        {
            this.worldMonitor = worldMonitor;
            this.portalStateStorage = portalStateStorage;
        }

        public string Interact(IInteractor interactor)
        {
            if (portalData == null)
            {
                PortalInformation portalInfo = portalRegistry.TryGetPortal(
                    portalStateStorage.PortalId,
                    out var info
                )
                    ? info
                    : null;
                portalData = portalInfo.Data;
            }

            uint playerNetId = interactor.NetId;

            var connId = GetPlayerConnectionId(playerNetId);
            if (!connId.HasValue)
            {
                logger?.Log($"[PortalInteractSystem] conn not found, Player:{playerNetId}.", this);
                return portalData.id;
            }

            worldMonitor.Events.Enqueue(
                new OpenPortalEvent(
                    playerNetId,
                    new OpenPortalEventContent
                    {
                        PortalId = portalData.id,
                        DestinationName = portalData.targetPortalName,
                    },
                    connId.Value
                )
            );

            worldMonitor.Events.Enqueue(new InteractCompletedEvent(playerNetId, connId.Value));

            logger?.Log(
                $"[PortalInteractSystem] Player {playerNetId} interacted with portal '{portalData.id}'.",
                this
            );

            return portalData.id;
        }

        // TODO: move this to a utility class since it's used in ShopInteractSystem as well
        private int? GetPlayerConnectionId(uint playerNetId)
        {
            if (
                worldMonitor.Entities.TryGet(playerNetId, out var entity)
                && entity.ConnectionId.HasValue
            )
            {
                return entity.ConnectionId.Value;
            }
            return null;
        }
    }
}
