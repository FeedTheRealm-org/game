using FTR.Core.Common.EventChannels;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controls the UI popup for the quest prompt.
/// Called by QuestView when a quest is offered. Raises QuestDecisionEvent on accept/reject.
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
        if (_acceptButton != null)
            _acceptButton.clicked += OnAcceptClicked;
        if (_rejectButton != null)
            _rejectButton.clicked += OnRejectClicked;
    }

    private void OnDisable()
    {
        if (_acceptButton != null)
            _acceptButton.clicked -= OnAcceptClicked;
        if (_rejectButton != null)
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
    /// Populates and shows the quest prompt panel.
    /// Called by QuestView via QuestOfferedEvent.
    /// </summary>
    public void OnQuestOffered(QuestData data)
    {
        Debug.Log(
            $"[QuestPromptController] OnQuestOffered called inside PromptController for quest: {data?.title}"
        );
        if (data == null)
            return;

        _currentQuestData = data;
        _titleLabel.text = data.title;
        _descriptionLabel.text = data.content;
        ToggleQuestPrompt(true);
    }

    private void OnAcceptClicked()
    {
        if (_currentQuestData == null)
            return;

        logger.Log($"Quest '{_currentQuestData.title}' was accepted.", this);
        questDecisionEvent.Raise(new QuestDecisionData(_currentQuestData, true));
        ToggleQuestPrompt(false);
        _currentQuestData = null;
    }

    private void OnRejectClicked()
    {
        if (_currentQuestData == null)
            return;

        logger.Log($"Quest '{_currentQuestData.title}' was rejected.", this);
        questDecisionEvent.Raise(new QuestDecisionData(_currentQuestData, false));
        ToggleQuestPrompt(false);
        _currentQuestData = null;
    }
}
