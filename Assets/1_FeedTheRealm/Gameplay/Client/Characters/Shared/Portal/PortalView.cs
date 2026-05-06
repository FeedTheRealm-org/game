using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using API;
using FeedTheRealm.Core.EventChannels.Setup;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Client.EventChannels.Portal;
using FTR.Core.Common.Config;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.EntryPoints;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace FTR.Gameplay.Common.Characters.Shared.Portal
{
    /// <summary>
    /// View component for the Portal. Listens to OpenPortalEvents and displays the portal UI when they are received.
    /// Lives on the Portal prefab as a child of the main Portal GameObject.
    /// </summary>
    public class PortalView : MonoBehaviour
    {
        [Inject]
        private OpenPortalUIEvent openPortalUiEvent;

        [Inject]
        private PortalToggleEvent portalToggleEvent;

        [Inject]
        private LoadingEvent loadingEvent;

        // ---------------------------------------------------------------------------------------------
        // These will be removed in the future when we refactor teleportation
        // to be server-driven and not have the client be in charge of loading scenes on teleport
        [Header("TEMPORARY TELEPORTATION FIELDS - TO BE REFACTORED")]
        [SerializeField]
        private WorldSelector worldSelector;

        [SerializeField]
        private SceneReference worldScene;

        [SerializeField]
        private TeleportDataPersistence teleportDataPersistence;

        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private WorldService worldService;

        [SerializeField]
        private PlayerService playerService;

        [SerializeField]
        private Config config;

        // ---------------------------------------------------------------------------------------------

        private NetworkEventRouter eventRouter;
        private NetworkAdapter networkAdapter;

        public void Initialize(NetworkEventRouter eventRouter, NetworkAdapter networkAdapter)
        {
            this.eventRouter = eventRouter;
            this.networkAdapter = networkAdapter;
            eventRouter.OnOpenPortalEvent += HandleOpenPortalRequest;

            if (teleportDataPersistence.PortalId != null)
            {
                TeleportPlayer(teleportDataPersistence.PortalId);
                teleportDataPersistence.PortalId = null;
            }
        }

        private async Task AcceptPortalRequest(OpenPortalEventContent content, int destinationZone)
        {
            try
            {
                // TODO: CURRENT TELEPORT REFACTOR
                // for now, if teleporing between zones, we will have the client be in charge of doing so,
                // but this is unsafe and can lead to exploits regarding tp destination. This will be refactored in the future
                // but for now we will implement this way to progress with portal development and have a working version of it in the game.

                if (worldSelector.GetSelectedZoneId() != destinationZone)
                {
                    Debug.Log(
                        $"[PortalView] Teleporting to {content.PortalId} in zone {destinationZone}. Loading new scene."
                    );
                    NetworkManager.singleton.StopClient();
                    worldSelector.SetSelectedZoneId(destinationZone);
                    teleportDataPersistence.PortalId = content.PortalId;
                    var worldJoinToken = await playerService.IssueWorldJoinTokenAsync(
                        worldSelector.GetSelectedWorldId()
                    );
                    worldSelector.SetSelectedWorldJoinToken(worldJoinToken.token_id);

                    var (ip, port, error, statusCode) = await worldService.GetZoneAddress(
                        worldSelector.GetSelectedWorldId(),
                        destinationZone
                    );
                    config.CurrentServerAddress = ip;
                    config.CurrentServerPort = (ushort)port;

                    SceneManager.LoadScene(worldScene.SceneName);
                }
                else
                    TeleportPlayer(content.PortalId);

                portalToggleEvent.Raise(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PortalView] Failed to accept portal request: {ex.Message}");
                loadingEvent.Raise(false);
            }
        }

        private void RejectPortalRequest()
        {
            portalToggleEvent.Raise(false);
        }

        private void TeleportPlayer(string portalId)
        {
            networkAdapter.DispatchTransaction(
                new TransactionCommandDTO { Type = TransactionType.AcceptTeleport, Id = portalId }
            );
            TogglePortalLoading(true);
        }

        private void OnDestroy()
        {
            if (eventRouter != null)
                eventRouter.OnOpenPortalEvent -= HandleOpenPortalRequest;
        }

        private void HandleOpenPortalRequest(OpenPortalEventContent content)
        {
            Debug.Log($"[PortalView] HandleOpenPortalRequest received. {content.PortalName}");

            if (
                string.IsNullOrEmpty(content.PortalId)
                && string.IsNullOrEmpty(content.DestinationName)
            )
            {
                TogglePortalLoading(false);
                return;
            }

            // we raise an event to the UI to open the portal dialog,
            // and we pass it an action that will be called if the player accepts the portal request and the destination name to display on the UI.
            //  The action will send the AcceptTeleport transaction to the server with the correct portal id.
            OpenPortalUiContent uiContent = new()
            {
                DestinationName = content.DestinationName,
                PortalName = content.PortalName,
                OnAccept = async () => await AcceptPortalRequest(content, content.DestinationZone),
                OnReject = () => RejectPortalRequest(),
            };

            portalToggleEvent.Raise(true);
            openPortalUiEvent.Raise(uiContent);
        }

        private void TogglePortalLoading(bool isLoading)
        {
            loadingEvent.Raise(isLoading);
        }
    }
}
