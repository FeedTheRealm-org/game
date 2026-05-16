using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeedTheRealm.UI.Common
{
    /// <summary>
    /// Reusable confirm/cancel dialog controller.
    ///
    /// <code>
    ///     _confirmDialog.Show(
    ///         question:  "¿Are you sure to...?",
    ///         onConfirm: () => func(),
    ///         onCancel:  () => { /* optional */ },
    ///         title:     "Tittle"           // optional
    ///     );
    /// </code>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ConfirmDialogController : MonoBehaviour
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

        private void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.clicked -= OnConfirmClicked;
            if (_cancelButton != null)
                _cancelButton.clicked -= OnCancelClicked;
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
        }

        public void Hide()
        {
            _overlay.style.display = DisplayStyle.None;
            _onConfirm = null;
            _onCancel = null;
        }

        private void OnConfirmClicked()
        {
            var cb = _onConfirm;
            Hide();
            cb?.Invoke();
        }

        private void OnCancelClicked()
        {
            var cb = _onCancel;
            Hide();
            cb?.Invoke();
        }
    }
}
