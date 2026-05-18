using System;
using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Input;
using FTR.Core.Client.EventChannels.Portal;
using FTR.Core.Client.Managers;
using FTR.Core.Common.Characters;
using FTR.Core.Common.Enums;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.WorldSpace
{
    /// <summary>
    /// World-space UI controller for the NPC dialog box.
    /// Lives on the DialogBox prefab instantiated as a child of each NPC.
    /// Resolves its own NPC id by finding ICharacterIdentity in the parent hierarchy
    /// </summary>
    public class PortalRequestUI : MonoBehaviour
    {
        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private OpenPortalUIEvent openPortalEvent;

        [Inject]
        private MenuManager menuManager;

        [Inject]
        private BackEvent backEvent;

        private bool isOpen = false;

        private VisualElement root;
        private Label destinationLabel;
        private Label portalNameLabel;
        private Button acceptButton;
        private Button declineButton;
        private OpenPortalUiContent currentContent;
        private Action acceptClickedHandler;
        private Action declineClickedHandler;

        private void Awake()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            portalNameLabel = root.Q<Label>("PortalName");
            destinationLabel = root.Q<Label>("Destination");
            acceptButton = root.Q<Button>("AcceptButton");
            declineButton = root.Q<Button>("DeclineButton");
            root.style.display = DisplayStyle.None;
            backEvent.OnRaised += OnClose;
        }

        private void OnDestroy()
        {
            backEvent.OnRaised -= OnClose;
        }

        private void OnEnable()
        {
            openPortalEvent.OnRaised += HandleOpenPortalRequest;
        }

        private void OnDisable()
        {
            openPortalEvent.OnRaised -= HandleOpenPortalRequest;

            if (acceptClickedHandler != null)
                acceptButton.clicked -= acceptClickedHandler;

            if (declineClickedHandler != null)
                declineButton.clicked -= declineClickedHandler;
        }

        private void HandleOpenPortalRequest(OpenPortalUiContent content)
        {
            logger.Log($"[PortalRequestController] Received OpenPortalRequest", this);

            if (isOpen || !menuManager.CanOpenMenu(MenuType.Portal))
                return;

            destinationLabel.text = content.DestinationName;
            portalNameLabel.text = content.PortalName;
            root.style.display = DisplayStyle.Flex;
            isOpen = true;
            menuManager.ToggleMenu(MenuType.Portal, true);
            currentContent = content;

            if (acceptClickedHandler != null)
                acceptButton.clicked -= acceptClickedHandler;
            if (declineClickedHandler != null)
                declineButton.clicked -= declineClickedHandler;

            acceptClickedHandler = () => HandleButtonClicked(currentContent);
            declineClickedHandler = () => HandleButtonClicked(currentContent, false);

            acceptButton.clicked += acceptClickedHandler;
            declineButton.clicked += declineClickedHandler;
        }

        private void HandleButtonClicked(OpenPortalUiContent content, bool isAccept = true)
        {
            CloseMenu();

            if (isAccept)
                content.OnAccept?.Invoke();
            else
                content.OnReject?.Invoke();
        }

        private void OnClose()
        {
            if (!isOpen)
                return;

            CloseMenu();
            currentContent?.OnReject?.Invoke();
        }

        private void CloseMenu()
        {
            if (!isOpen)
                return;

            root.style.display = DisplayStyle.None;
            destinationLabel.text = string.Empty;
            portalNameLabel.text = string.Empty;
            currentContent = null;
            isOpen = false;
            menuManager.ToggleMenu(MenuType.Portal, false);
        }
    }
}
