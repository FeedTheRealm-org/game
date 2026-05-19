using System;
using FTR.Core.Client.EventChannels.Input;
using FTR.Core.Client.Managers;
using FTR.Core.Common.EventChannels;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// Controls the UI popup for the quest prompt.
/// Called by QuestView when a quest is offered. Raises QuestDecisionEvent on accept/reject.
/// </summary>
public class QuestPromptController : MonoBehaviour
{
    [Header("Event Registry")]
    [SerializeField]
    private CommonEventRegistry eventRegistry;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    [Inject]
    private MenuManager menuManager;

    [Inject]
    private BackEvent backEvent;

    private VisualElement _root;
    private Label _titleLabel;
    private Label _descriptionLabel;
    private Button _acceptButton;
    private Button _rejectButton;

    private QuestData _currentQuestData;
    private uint _netId;
    private string _npcId;

    public void Initialize(uint netId)
    {
        _netId = netId;
    }

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

        backEvent.OnRaised += OnRejectClicked;
        ToggleQuestPrompt(false);
    }

    private void OnDestroy()
    {
        backEvent.OnRaised -= OnRejectClicked;
    }

    private void OnEnable()
    {
        if (_acceptButton != null)
            _acceptButton.clicked += OnAcceptClicked;
        if (_rejectButton != null)
            _rejectButton.clicked += OnRejectClicked;

        if (eventRegistry != null && eventRegistry.showQuestPromptEvent != null)
        {
            eventRegistry.showQuestPromptEvent.OnRaised += OnQuestOffered;
        }
        else
        {
            throw new MissingFieldException(
                "CommonEventRegistry or ShowQuestPromptEvent is not assigned in the inspector."
            );
        }
    }

    private void OnDisable()
    {
        if (_acceptButton != null)
            _acceptButton.clicked -= OnAcceptClicked;
        if (_rejectButton != null)
            _rejectButton.clicked -= OnRejectClicked;

        if (eventRegistry != null && eventRegistry.showQuestPromptEvent != null)
            eventRegistry.showQuestPromptEvent.OnRaised -= OnQuestOffered;
    }

    /// <summary>
    /// Toggles the visibility of the quest prompt panel.
    /// </summary>
    public void ToggleQuestPrompt(bool show)
    {
        _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        menuManager.ToggleMenu(MenuType.Quest, show);
    }

    /// <summary>
    /// Populates and shows the quest prompt panel.
    /// Called by QuestView via showQuestPromptEvent.
    /// </summary>
    public void OnQuestOffered(QuestPromptData promptData)
    {
        if (promptData.Quest == null || !menuManager.CanOpenMenu(MenuType.Quest))
            return;

        _currentQuestData = promptData.Quest;
        _netId = promptData.TargetNetId;
        _npcId = promptData.NpcId;
        _titleLabel.text = _currentQuestData.title;
        _descriptionLabel.text = _currentQuestData.content;
        ToggleQuestPrompt(true);
    }

    private void OnAcceptClicked()
    {
        if (_currentQuestData == null)
            return;

        logger?.Log(
            $"Quest '{_currentQuestData.title}' was accepted. Dispatched by NetId: {_netId}",
            this
        );
        if (eventRegistry != null && eventRegistry.questDecisionEvent != null)
            eventRegistry.questDecisionEvent.Raise(
                new QuestDecisionData(_currentQuestData, true, _netId, _npcId)
            );

        ToggleQuestPrompt(false);
        _currentQuestData = null;
    }

    private void OnRejectClicked()
    {
        if (_currentQuestData == null)
            return;

        logger?.Log(
            $"Quest '{_currentQuestData.title}' was rejected. Dispatched by NetId: {_netId}",
            this
        );
        if (eventRegistry != null && eventRegistry.questDecisionEvent != null)
            eventRegistry.questDecisionEvent.Raise(
                new QuestDecisionData(_currentQuestData, false, _netId, _npcId)
            );

        ToggleQuestPrompt(false);
        _currentQuestData = null;
    }
}
