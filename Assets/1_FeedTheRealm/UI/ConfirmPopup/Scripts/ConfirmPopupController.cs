using System;
using FTR.Core.Client.EventChannels.Input;
using FTR.Core.Client.Interfaces;
using FTR.Core.Client.Managers;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI
{
    /// <summary>
    /// Reusable confirm/cancel dialog controller.
    ///
    /// Supports two usage modes:
    ///
    /// MANAGED MODE (World scene): Instantiated via objectResolver, receives MenuManager
    /// and BackEvent by injection. Persists in the scene, never destroyed.
    /// _confirmPopup.Show(...) from injected ConfirmPopupHandle.
    ///
    /// STANDALONE MODE (Main Menu scene): Instantiated via normal Instantiate, without
    /// injection. Does not coordinate with MenuManager or BackEvent. Destroyed upon closing.
    /// var popup = Instantiate(prefab).GetComponent<IConfirmPopup>();
    /// popup.Show(...);
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ConfirmPopupController : MonoBehaviour, IConfirmPopup
    {
        private VisualElement _overlay;
        private Label _titleLabel;
        private Label _questionLabel;
        private Button _confirmButton;
        private Button _cancelButton;

        private Action _onConfirm;
        private Action _onCancel;

        [SerializeField]
        private string defaultConfirmText = "Confirm";

        [SerializeField]
        private string defaultCancelText = "Cancel";

        // Opcionales: solo presentes cuando el container los inyecta (modo managed)
        [Inject]
        private MenuManager menuManager;

        [Inject]
        private BackEvent backEvent;

        private bool isManaged => menuManager != null;

        private void Awake()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            root.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

            _overlay = root.Q<VisualElement>("Overlay");
            _titleLabel = root.Q<Label>("DialogTitle");
            _questionLabel = root.Q<Label>("QuestionLabel");
            _confirmButton = root.Q<Button>("ConfirmButton");
            _cancelButton = root.Q<Button>("CancelButton");

            _confirmButton.clicked += OnConfirmClicked;
            _cancelButton.clicked += OnCancelClicked;

            Hide();
        }

        private void Start()
        {
            // Solo en modo managed: suscribirse al BackEvent para interceptar ESC
            if (backEvent != null)
                backEvent.OnRaised += OnBackPressed;
        }

        private void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.clicked -= OnConfirmClicked;
            if (_cancelButton != null)
                _cancelButton.clicked -= OnCancelClicked;
            if (backEvent != null)
                backEvent.OnRaised -= OnBackPressed;
        }

        public void Show(
            string question,
            Action onConfirm,
            Action onCancel = null,
            string title = "Confirm Action",
            string confirmText = null,
            string cancelText = null
        )
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            _titleLabel.text = title;
            _questionLabel.text = question;

            _confirmButton.text = string.IsNullOrEmpty(confirmText)
                ? defaultConfirmText
                : confirmText;
            _cancelButton.text = string.IsNullOrEmpty(cancelText) ? defaultCancelText : cancelText;

            _overlay.style.display = DisplayStyle.Flex;

            if (isManaged)
                menuManager.ToggleMenu(MenuType.Confirmation, true);
        }

        public void Hide()
        {
            _overlay.style.display = DisplayStyle.None;
            _onConfirm = null;
            _onCancel = null;

            if (isManaged)
                menuManager.ToggleMenu(MenuType.Confirmation, false);
        }

        /// <summary>
        /// ESC: solo activo en modo managed. Cancela el popup sin afectar el menú de fondo.
        /// </summary>
        private void OnBackPressed()
        {
            if (_overlay.style.display != DisplayStyle.Flex)
                return;

            var cb = _onCancel;
            Hide();
            cb?.Invoke();
        }

        private void OnConfirmClicked()
        {
            var cb = _onConfirm;
            Hide();
            cb?.Invoke();

            if (!isManaged)
                Destroy(gameObject);
        }

        private void OnCancelClicked()
        {
            var cb = _onCancel;
            Hide();
            cb?.Invoke();

            if (!isManaged)
                Destroy(gameObject);
        }
    }
}
