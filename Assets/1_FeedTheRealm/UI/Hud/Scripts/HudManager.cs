using Game.Core.Events;
using Game.Core.Quests;
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
    private GameObject questPromptPanel;

    [Header("Quests")]
    [SerializeField]
    private QuestOfferedEvent questOfferedEvent;

    [SerializeField]
    private QuestDecisionEvent questDecisionEvent;

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
    }

    private void OnEnable()
    {
        ToggleHud(true);
        ToggleQuestPrompt(false);

        questOfferedEvent.OnRaised += OnQuestOffered;
    }

    private void OnDisable()
    {
        questOfferedEvent.OnRaised -= OnQuestOffered;
    }

    /// <summary>
    /// Toggles the visibility of the HUD panel and quest prompt panel.
    /// </summary>
    private void ToggleHud(bool show)
    {
        if (hudPanel == null)
            return;
        hudPanel.SetActive(show);
        ToggleQuestPrompt(show);
    }

    /// <summary>
    /// Toggles the visibility of the quest prompt panel.
    /// </summary>
    private void ToggleQuestPrompt(bool show)
    {
        if (questPromptPanel == null)
            return;
        questPromptPanel.SetActive(show);
    }

    private void OnQuestOffered(QuestData data)
    {
        ToggleQuestPrompt(true);
    }
}
