using FeedTheRealm.Core.Interfaces;
using FTR.Core.Common.Characters;
using FTR.Core.Common.EventChannels;
using FTR.Gameplay.Client.Registry;
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
    public class UIDialogController : MonoBehaviour, IDialogBox
    {
        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        private ISoundPlayer soundPlayer;

        private VisualElement _root;
        private Label _msgLabel;
        private Label _senderLabel;

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
        }

        private void OnEnable()
        {
            ui = GetComponent<UIDocument>().rootVisualElement;
        }

        public void ShowDialogMessage(MessageData message)
        {
            _msgLabel.text = message.content;
            _senderLabel.text = SenderPrefix + message.sender;
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.Dialog);
        }

        public void ToggleDialog(bool isOpen)
        {
            _root.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.Dialog);
        }
    }
}
