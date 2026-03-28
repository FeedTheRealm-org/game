using FTR.Core.Common.EventChannels;
using FTR.Gameplay.Common.Environment.Npcs;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.WorldSpace
{
    /// <summary>
    /// World-space UI controller for the NPC dialog box.
    /// Lives on the DialogBox prefab which is instantiated as a child of each NPC.
    /// Listens to dialog EventChannel SOs and filters by NpcIdentity.NpcId so only
    /// the correct NPC's box reacts to dialog events raised by the local player's InteractView.
    /// </summary>
    public class UIDialogController : MonoBehaviour
    {
        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        private NpcDialogMessageEvent npcDialogMessageEvent;

        [Inject]
        private NpcDialogToggledEvent npcDialogToggledEvent;
        private VisualElement ui;

        private INpcIdentity npcIdentity;
        private VisualElement _root;
        private Label _msgLabel;
        private Label _senderLabel;

        private const string SenderPrefix = " - ";

        private void Awake()
        {
            npcIdentity = GetComponentInParent<INpcIdentity>();
            ui = GetComponent<UIDocument>().rootVisualElement;
        }

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _msgLabel = _root.Q<Label>("DialogText");
            _senderLabel = _root.Q<Label>("DialogSender");

            if (_msgLabel == null || _senderLabel == null)
            {
                logger.Log(
                    "Could not find required UI elements (DialogText / DialogSender) in UIDialogController.",
                    this,
                    Logging.LogType.Error
                );
            }

            _root.style.display = DisplayStyle.None;
        }

        private void OnEnable()
        {
            ui = GetComponent<UIDocument>().rootVisualElement;
            npcDialogMessageEvent.OnRaised += HandleDialogChanged;
            npcDialogToggledEvent.OnRaised += HandleToggleDialog;
        }

        private void OnDisable()
        {
            npcDialogMessageEvent.OnRaised -= HandleDialogChanged;
            npcDialogToggledEvent.OnRaised -= HandleToggleDialog;
        }

        private void HandleDialogChanged((string npcId, MessageData message) data)
        {
            if (!IsMyNpc(data.npcId))
                return;

            //logger.Log($"[UIDialogController] Dialog changed: {data.message.Content}", this);
            _msgLabel.text = data.message.Content;
            _senderLabel.text = SenderPrefix + data.message.Sender;
        }

        private void HandleToggleDialog((bool isOpen, string npcId) data)
        {
            if (!IsMyNpc(data.npcId))
                return;
            _root.style.display = data.isOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private bool IsMyNpc(string npcId)
        {
            if (npcIdentity == null)
            {
                logger.Log(
                    "[UIDialogController] NpcIdentity not found in parent.",
                    this,
                    Logging.LogType.Error
                );
                return false;
            }

            return !string.IsNullOrEmpty(npcIdentity.NpcId) && npcIdentity.NpcId == npcId;
        }
    }
}
