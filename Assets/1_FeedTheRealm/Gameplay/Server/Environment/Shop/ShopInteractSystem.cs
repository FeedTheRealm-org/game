using FTR.Core.Common.Interactions;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class ShopInteractSystem : MonoBehaviour, IInteractable
    {
        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        private string shopId;
        private WorldMonitor worldMonitor;
        private uint ownNetId;

        public void Initialize(
            Logging.Logger logger,
            WorldMonitor worldMonitor,
            uint ownNetId,
            string shopId
        )
        {
            this.logger = logger;
            this.worldMonitor = worldMonitor;
            this.ownNetId = ownNetId;
            this.shopId = shopId;
        }

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

        public string Interact(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            var connId = GetPlayerConnectionId(playerNetId);
            if (!connId.HasValue)
            {
                logger?.Log($"[ShopInteractSystem] conn not found, Player:{playerNetId}.", this);
                return shopId;
            }

            worldMonitor.Events.Enqueue(
                new OpenShopEvent(
                    ownNetId,
                    new OpenShopEventContent { ShopId = shopId },
                    connId.Value
                )
            );

            logger?.Log($"[ShopInteractSystem] Player {playerNetId} opened shop '{shopId}'.", this);

            return shopId;
        }

        public void ContinueInteraction(IInteractor interactor) { }

        public void StopInteraction(IInteractor interactor) { }

        public bool CanInteract(IInteractor interactor)
        {
            return true;
        }
    }
}
