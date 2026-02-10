using Game.Core.Client.Events;
using Game.Core.Client.Quests;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controls the UI popup for the quest prompt.
/// </summary>
public class QuestPromptController : MonoBehaviour
{
    [Header("Quests")]
    [SerializeField]
    private QuestDecisionEvent questDecisionEvent;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private VisualElement _root;

    private Label _titleLabel;
    private Label _descriptionLabel;
    private Button _acceptButton;
    private Button _rejectButton;

    private QuestData _currentQuestData;

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _titleLabel = _root.Q<Label>("QuestTitle");
        _descriptionLabel = _root.Q<Label>("QuestDescription");
        _acceptButton = _root.Q<Button>("AcceptButton");
        _rejectButton = _root.Q<Button>("RejectButton");
        if (
            _titleLabel == null
            || _descriptionLabel == null
            || _acceptButton == null
            || _rejectButton == null
        )
            logger.Log(
                "One or more UI elements are not assigned in the inspector.",
                this,
                Logging.LogType.Error
            );
        ToggleQuestPrompt(false);
    }

    private void OnEnable()
    {
        _acceptButton.clicked += OnAcceptClicked;
        _rejectButton.clicked += OnRejectClicked;
    }

    private void OnDisable()
    {
        _acceptButton.clicked -= OnAcceptClicked;
        _rejectButton.clicked -= OnRejectClicked;
    }

    /// <summary>
    /// Toggles the visibility of the quest prompt panel.
    /// </summary>
    public void ToggleQuestPrompt(bool show)
    {
        _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// Sets the quest to show on the pannel.
    /// </summary>
    public void OnQuestOffered(QuestData data)
    {
        _titleLabel.text = data.Title;
        _descriptionLabel.text = data.Content;
        _currentQuestData = data;
    }

    private void OnAcceptClicked()
    {
        logger.Log($"Quest {_currentQuestData.Title} was accepted", this);
        questDecisionEvent.Raise(new QuestDecisionData(_currentQuestData, true));
    }

    private void OnRejectClicked()
    {
        logger.Log($"Quest {_currentQuestData.Title} was rejected", this);
        questDecisionEvent.Raise(new QuestDecisionData(_currentQuestData, false));
    }
}
