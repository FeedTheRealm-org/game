using System;
using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Portal;
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

        private bool isOpen = false;

        private VisualElement root;
        private Label destinationLabel;
        private Label portalNameLabel;
        private Button acceptButton;
        private Button declineButton;

        private void Awake()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            portalNameLabel = root.Q<Label>("PortalName");
            destinationLabel = root.Q<Label>("Destination");
            acceptButton = root.Q<Button>("AcceptButton");
            declineButton = root.Q<Button>("DeclineButton");
            root.style.display = DisplayStyle.None;
        }

        private void OnEnable()
        {
            openPortalEvent.OnRaised += HandleOpenPortalRequest;
        }

        private void OnDisable()
        {
            openPortalEvent.OnRaised -= HandleOpenPortalRequest;
            acceptButton.clicked -= () => HandleButtonClicked(null);
            declineButton.clicked -= () => HandleButtonClicked(null);
        }

        private void HandleOpenPortalRequest(OpenPortalUiContent content)
        {
            logger.Log($"[PortalRequestController] Received OpenPortalRequest", this);

            if (isOpen)
                return;

            destinationLabel.text = content.DestinationName;
            portalNameLabel.text = content.PortalName;
            root.style.display = DisplayStyle.Flex;
            isOpen = true;

            acceptButton.clicked += () => HandleButtonClicked(content);
            declineButton.clicked += () => HandleButtonClicked(content, false);
        }

        private void HandleButtonClicked(OpenPortalUiContent content, bool isAccept = true)
        {
            root.style.display = DisplayStyle.None;
            destinationLabel.text = string.Empty;

            if (isAccept)
                content.OnAccept?.Invoke();
            else
                content.OnReject?.Invoke();
            isOpen = false;
        }
    }
}
