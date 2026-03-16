using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI controller for the NPC dialog box.
/// Subscribes to InteractView events and updates visual elements accordingly.
/// </summary>
public class UIDialogController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private InteractView interactView;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private VisualElement _root;
    private Label _msgLabel;
    private Label _senderLabel;

    private const string SenderPrefix = " - ";

    private void Start()
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
        interactView.OnDialogMessageChanged += HandleDialogChanged;
        interactView.OnDialogToggled += HandleToggleDialog;
    }

    private void OnDisable()
    {
        interactView.OnDialogMessageChanged -= HandleDialogChanged;
        interactView.OnDialogToggled -= HandleToggleDialog;
    }

    private void HandleDialogChanged(MessageData message)
    {
        logger.Log($"Dialog changed: {message.Content}", this);
        _msgLabel.text = message.Content;
        _senderLabel.text = SenderPrefix + message.Sender;
    }

    private void HandleToggleDialog(bool isOpen)
    {
        _root.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
