using FTR.Core.Client.EventChannels.Chat;
using FTR.Core.Client.EventChannels.Input;
using FTR.Core.Client.Managers;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.WorldSpace
{
    [RequireComponent(typeof(UIDocument))]
    public class ChatInputUIController : MonoBehaviour
    {
        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private PlayerInputReader inputReader;

        [Inject]
        private ChatMessageRequestEvent chatMessageRequestEvent;

        [Inject]
        private ChatToggleEvent chatToggleEvent;

        [Inject]
        private MenuManager menuManager;

        private VisualElement _root;
        private VisualElement _inputPanel;
        private TextField _inputField;

        private bool _isOpen = true;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _inputPanel = _root.Q<VisualElement>("ChatInputPanel");
            _inputField = _root.Q<TextField>("ChatInputField");

            if (_inputPanel == null || _inputField == null)
            {
                logger?.Log(
                    "[ChatInputUIController] Could not find required UI elements "
                        + "(ChatInputPanel / ChatInputField).",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            CloseInput();
        }

        private void OnEnable()
        {
            if (inputReader != null)
                inputReader.ChatToggleEvent += OnChatToggle;
            menuManager.RegisterMenuCallbacks(MenuType.Chat, onOpen: null, onClose: CloseInput);
        }

        private void OnDisable()
        {
            if (inputReader != null)
                inputReader.ChatToggleEvent -= OnChatToggle;
        }

        private void OnChatToggle()
        {
            chatToggleEvent?.Raise(!_isOpen);
            if (_isOpen)
                TrySendMessage();
            else
                OpenInput();
        }

        private void OpenInput()
        {
            if (_isOpen || !menuManager.CanOpenMenu(MenuType.Chat))
                return;

            _isOpen = true;
            _inputPanel.style.display = DisplayStyle.Flex;
            _inputField.value = string.Empty;
            _inputField.Focus();

            menuManager.ToggleMenu(MenuType.Chat, true);
        }

        private void CloseInput()
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            _inputPanel.style.display = DisplayStyle.None;
            _inputField.value = string.Empty;

            menuManager.ToggleMenu(MenuType.Chat, false);
        }

        // ── Send ──────────────────────────────────────────────────────────────

        private void TrySendMessage()
        {
            string message = _inputField.value?.Trim();

            if (string.IsNullOrEmpty(message))
            {
                CloseInput();
                return;
            }

            logger?.Log($"[ChatInputUIController] Sending message: \"{message}\"", this);

            chatMessageRequestEvent?.Raise(message);

            CloseInput();
        }
    }
}
