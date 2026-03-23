using FTR.Core.Common.EventChannels;
using FTRShared.Runtime.Models;
using UnityEngine;

/// <summary>
/// Manages the HUD elements and their interactions.
/// </summary>
public class HudManager : MonoBehaviour
{
    [Header("HUD settings")]
    [SerializeField]
    private GameObject hudPanel;

    [SerializeField]
    private QuestPromptController questPromptPanel;

    [SerializeField]
    private QuestCompletionPanelController questCompletionPanel;

    [Header("Quests")]
    [SerializeField]
    private QuestOfferedEvent questOfferedEvent;

    [SerializeField]
    private QuestDecisionEvent questDecisionEvent;

    [SerializeField]
    private QuestCompletedEvent questCompletedEvent;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private void Start()
    {
        if (hudPanel == null)
            logger.Log("HUD Panel is not assigned in the inspector.", this, Logging.LogType.Error);

        if (questPromptPanel == null)
            logger.Log(
                "Quest Prompt Panel is not assigned in the inspector.",
                this,
                Logging.LogType.Error
            );

        if (questOfferedEvent == null)
            logger.Log(
                "Quest Offered Event is not assigned in the inspector.",
                this,
                Logging.LogType.Error
            );

        if (questDecisionEvent == null)
            logger.Log(
                "Quest Decision Event is not assigned in the inspector.",
                this,
                Logging.LogType.Error
            );

        if (questCompletedEvent == null)
            logger.Log(
                "Quest Completed Event is not assigned in the inspector.",
                this,
                Logging.LogType.Error
            );
    }

    private void OnEnable()
    {
        hudPanel.SetActive(true);
        questOfferedEvent.OnRaised += OnQuestOffered;
        questDecisionEvent.OnRaised += OnQuestDecision;
        questCompletedEvent.OnRaised += OnQuestCompleted;
    }

    private void OnDisable()
    {
        questOfferedEvent.OnRaised -= OnQuestOffered;
        questDecisionEvent.OnRaised -= OnQuestDecision;
        questCompletedEvent.OnRaised -= OnQuestCompleted;
    }

    private void OnQuestOffered(QuestData data)
    {
        questPromptPanel.ToggleQuestPrompt(true);
        questPromptPanel.OnQuestOffered(data);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnQuestDecision(QuestDecisionData _)
    {
        questPromptPanel.ToggleQuestPrompt(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnQuestCompleted(QuestData data)
    {
        questCompletionPanel.ToggleQuestCompletionPanel(true);
        questCompletionPanel.OnQuestCompleted(data);
    }
}
