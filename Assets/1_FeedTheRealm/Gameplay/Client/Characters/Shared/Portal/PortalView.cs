using System;
using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Portal;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Characters.Shared.Portal
{
    /// <summary>
    /// View component for the Portal. Listens to OpenPortalEvents and displays the portal UI when they are received.
    /// Lives on the Portal prefab as a child of the main Portal GameObject.
    /// </summary>
    public class PortalView : MonoBehaviour
    {
        [SerializeField]
        private OpenPortalUIEvent openPortalUiEvent;
        private NetworkEventRouter eventRouter;
        private NetworkAdapter networkAdapter;

        public void Initialize(NetworkEventRouter eventRouter, NetworkAdapter networkAdapter)
        {
            this.eventRouter = eventRouter;
            this.networkAdapter = networkAdapter;
            eventRouter.OnOpenPortalEvent += HandleOpenPortalRequest;
        }

        private void AcceptPortalRequest(string portalId)
        {
            networkAdapter.DispatchTransaction(
                new TransactionCommandDTO { Type = TransactionType.AcceptTeleport, Id = portalId }
            );
        }

        private void OnDestroy()
        {
            eventRouter.OnOpenPortalEvent -= HandleOpenPortalRequest;
        }

        private void HandleOpenPortalRequest(OpenPortalEventContent content)
        {
            Debug.Log($"[PortalView] HandleOpenPortalRequest received.");

            // we raise an event to the UI to open the portal dialog,
            // and we pass it an action that will be called if the player accepts the portal request and the destination name to display on the UI.
            //  The action will send the AcceptTeleport transaction to the server with the correct portal id.
            OpenPortalUiContent uiContent = new()
            {
                DestinationName = content.DestinationName,
                OnAccept = () => AcceptPortalRequest(content.PortalId),
            };

            openPortalUiEvent.Raise(uiContent);
        }
    }
}
