using System;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Portal;
using FTR.Gameplay.Server.Environment.Quest;
using FTRShared.Runtime.Models;
using Mirror;
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

        private PortalInformation portalInfo = null;
        private WorldMonitor worldMonitor;

        public bool CanInteract(IInteractor interactor)
        {
            return true;
        }

        public void ContinueInteraction(IInteractor interactor)
        {
            TogglePortalRequest(interactor);
        }

        public void StopInteraction(IInteractor interactor)
        {
            return;
        }

        public void Initialize(WorldMonitor worldMonitor, PortalStateStorage portalStateStorage)
        {
            this.worldMonitor = worldMonitor;

            var portalId = portalStateStorage.portalId;
            portalInfo = portalRegistry.TryGetPortal(portalId, out var info) ? info : null;
            logger.Log($"[PortalInteractSystem] Initializing with Portal ID: {portalId}.", this);

            logger.Log($"[PortalInteractSystem] Portal data: {portalId}.", this);

            if (portalInfo == null)
            {
                logger.Log(
                    $"[PortalInteractSystem] Portal information not found for ID: {portalId}.",
                    this
                );
            }
            else
            {
                logger.Log(
                    $"[PortalInteractSystem] Portal information updated for ID: {portalId}.",
                    this
                );
            }
        }

        public string Interact(IInteractor interactor)
        {
            TogglePortalRequest(interactor);
            return portalInfo.Id;
        }

        // If executed once, it will open the portal request UI. If executed again while the UI is open, it will close it.
        private void TogglePortalRequest(IInteractor interactor)
        {
            try
            {
                logger?.Log(
                    $"[PortalInteractSystem] Interact called by Player:{interactor.NetId}.",
                    this
                );
                uint playerNetId = interactor.NetId;
                var connId = GetPlayerConnectionId(playerNetId);
                if (!connId.HasValue)
                {
                    logger.Log(
                        $"[PortalInteractSystem] conn not found, Player:{playerNetId}.",
                        this
                    );
                    return;
                }

                // Validate that the player is within the portal radius before allowing them to interact with it.
                if (NetworkServer.spawned.TryGetValue(playerNetId, out NetworkIdentity identity))
                {
                    GameObject player = identity.gameObject;
                    if (
                        player.transform.position != null
                        && Vector3.Distance(
                            player.transform.position,
                            portalInfo.PlacementData.position
                        ) > portalInfo.PlacementData.radius
                    )
                    {
                        logger.Log(
                            $"[PortalInteractSystem] Player {playerNetId} is too far from the portal to interact. Distance: {Vector3.Distance(player.transform.position, portalInfo.PlacementData.position)}, Allowed Radius: {portalInfo.PlacementData.radius}",
                            this
                        );
                        return;
                    }
                }
                else
                {
                    logger.Log(
                        $"[PortalInteractSystem] Player GameObject not found for NetId: {playerNetId}.",
                        this
                    );
                    return;
                }

                worldMonitor.Events.Enqueue(
                    new OpenPortalEvent(
                        playerNetId,
                        new OpenPortalEventContent
                        {
                            PortalId = portalInfo.Id,
                            DestinationName = portalInfo.DestinationName,
                        },
                        connId.Value
                    )
                );
                worldMonitor.Events.Enqueue(new InteractCompletedEvent(playerNetId, connId.Value));
                logger?.Log(
                    $"[PortalInteractSystem] Player {playerNetId} interacted with portal '{portalInfo.Id}'.",
                    this
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[PortalInteractSystem] An error occured during teleportation: {ex.Message}"
                );
            }
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
