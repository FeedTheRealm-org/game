using FTR.Core.Common.Characters;
using FTR.Core.Common.EventChannels;
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
    public class UIDialogController : MonoBehaviour
    {
        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        private NpcDialogMessageEvent npcDialogMessageEvent;

        [Inject]
        private NpcDialogToggledEvent npcDialogToggledEvent;

        private VisualElement _root;
        private Label _msgLabel;
        private Label _senderLabel;

        private string _targetNpcId;

        private const string SenderPrefix = " - ";

        private VisualElement ui;

        private void Awake()
        {
            ui = GetComponent<UIDocument>().rootVisualElement;
        }

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _msgLabel = _root.Q<Label>("DialogText");
            _senderLabel = _root.Q<Label>("DialogSender");

            if (_msgLabel == null || _senderLabel == null)
                logger.Log(
                    "Could not find required UI elements (DialogText / DialogSender).",
                    this,
                    Logging.LogType.Error
                );

            _root.style.display = DisplayStyle.None;

            var identity = GetComponentInParent<ICharacterIdentity>();
            if (identity == null)
            {
                logger.Log(
                    "[UIDialogController] Could not find ICharacterIdentity in parent hierarchy.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            if (!string.IsNullOrEmpty(identity.CharacterId))
            {
                SetNpcId(identity.CharacterId);
            }
            else
            {
                identity.OnCharacterIdChanged += OnCharacterIdReady;
            }
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

        public void BindNpc(string npcId) => SetNpcId(npcId);

        private void OnCharacterIdReady(string newId)
        {
            if (string.IsNullOrEmpty(newId))
                return;

            var identity = GetComponentInParent<ICharacterIdentity>();
            if (identity != null)
                identity.OnCharacterIdChanged -= OnCharacterIdReady;

            SetNpcId(newId);
        }

        private void SetNpcId(string npcId)
        {
            _targetNpcId = npcId;
            Debug.Log($"[UIDialogController] Bound to npcId: {_targetNpcId}");
        }

        private void HandleDialogChanged((string npcId, MessageData message) data)
        {
            if (!IsMyNpc(data.npcId))
                return;

            _msgLabel.text = data.message.content;
            _senderLabel.text = SenderPrefix + data.message.sender;
        }

        private void HandleToggleDialog((bool isOpen, string npcId) data)
        {
            if (!IsMyNpc(data.npcId))
                return;

            _root.style.display = data.isOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private bool IsMyNpc(string npcId)
        {
            if (string.IsNullOrEmpty(_targetNpcId))
            {
                logger.Log(
                    $"[UIDialogController] Received event for npcId={npcId} but this dialog has no id yet — event ignored.",
                    this,
                    Logging.LogType.Warning
                );
                return false;
            }

            return _targetNpcId == npcId;
        }
    }
}
