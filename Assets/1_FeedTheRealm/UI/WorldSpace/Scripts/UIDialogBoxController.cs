using Game.Core.Dialogue;
using UnityEngine;
using UnityEngine.UIElements;

public class UIDialogController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private DialogManagerComponent dialogManager;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    // Containers
    private VisualElement _root;

    private Label _msgLabel;
    private Label _senderLabel;

    private string _senderPrefix = " - ";

    void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        _msgLabel = _root.Q<Label>("DialogText");
        _senderLabel = _root.Q<Label>("DialogSender");
        if (_msgLabel == null || _senderLabel == null)
        {
            logger.Log(
                "Could not find required UI elements in UIDialogController.",
                this,
                Logging.LogType.Error
            );
        }

        _root.style.display = DisplayStyle.None;
    }

    private void OnEnable()
    {
        dialogManager.OnDialogChanged += HandleDialogChanged;
        dialogManager.OnToggleDialog += HandleToggleDialog;
    }

    private void OnDisable()
    {
        dialogManager.OnDialogChanged -= HandleDialogChanged;
        dialogManager.OnToggleDialog -= HandleToggleDialog;
    }

    private void HandleDialogChanged(Message message)
    {
        logger.Log($"Dialog changed: {message.Content}", this);
        _msgLabel.text = message.Content;
        _senderLabel.text = _senderPrefix + message.Sender;
    }

    private void HandleToggleDialog(bool isOpen)
    {
        _root.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
