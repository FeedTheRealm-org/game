using System.Collections;
using Game.Core.Client.Events;
using Game.Core.Client.Quests;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the quest progress panel in the UI, updating quest progress based on events.
/// </summary>
public class QuestProgressPanelController : MonoBehaviour
{
    [SerializeField]
    private QuestProgressEvent progressEvent;

    [SerializeField]
    private float completeQuestDisplayDuration = 3f;

    [SerializeField]
    private Logging.Logger logger;

    private ScrollView _currentQuestsContainer;

    private readonly string _questItemClasses = "quest-item";
    private readonly string _questTitleClasses = "quest-title";
    private readonly string _questProgressClasses = "quest-progress";
    private readonly float _questProgressHighValue = 100f;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _currentQuestsContainer = root.Q<ScrollView>("CurrentQuestsContainer");
        if (_currentQuestsContainer == null)
        {
            logger.Log(
                "CurrentQuestsContainer not found in the UI Document.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        if (progressEvent == null)
        {
            logger.Log(
                "QuestProgressEvent is not assigned in the inspector.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        _currentQuestsContainer.Clear();
        ToggleContainerVisibility(false);
    }

    private void OnEnable()
    {
        progressEvent.OnRaised += HandleQuestProgress;
    }

    private void OnDisable()
    {
        progressEvent.OnRaised -= HandleQuestProgress;
    }

    private void HandleQuestProgress(QuestProgressData questProgress)
    {
        logger.Log(
            $"New progress for quest {questProgress.Quest.Title} with id {questProgress.Id}",
            this
        );

        var questItem = _currentQuestsContainer.Q<VisualElement>(questProgress.Id);

        if (questItem == null)
        {
            questItem = CreateQuestItem(questProgress.Quest);
            ToggleContainerVisibility(true);
        }

        var progressBar = questItem.Q<ProgressBar>();
        if (progressBar == null)
        {
            logger.Log(
                $"Progress: Progress bar not found in quest item with ID {questProgress.Id}.",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        float percentComplete =
            ((float)questProgress.CurrentProgressAmount / questProgress.TargetProgressAmount)
            * _questProgressHighValue;

        progressBar.value = percentComplete;
        progressBar.title = $"{percentComplete}%";

        if (questProgress.CurrentProgressAmount >= questProgress.TargetProgressAmount)
            StartCoroutine(RemoveQuestAfterDelay(completeQuestDisplayDuration, questProgress.Id));
    }

    private VisualElement CreateQuestItem(QuestData questData)
    {
        var questItem = new VisualElement { name = questData.Id };
        questItem.AddToClassList(_questItemClasses);

        var titleLabel = new Label { text = questData.Title };
        titleLabel.AddToClassList(_questTitleClasses);
        questItem.Add(titleLabel);

        var progressBar = new ProgressBar
        {
            value = 0f,
            title = "0%",
            highValue = _questProgressHighValue,
        };
        progressBar.AddToClassList(_questProgressClasses);
        questItem.Add(progressBar);

        _currentQuestsContainer.Add(questItem);
        logger.Log(
            $"Created new quest item for quest {questData.Title} with id {questData.Id}",
            this
        );

        return questItem;
    }

    private void ToggleContainerVisibility(bool show)
    {
        if (_currentQuestsContainer.visible == show)
            return;

        _currentQuestsContainer.visible = show;
    }

    private IEnumerator RemoveQuestAfterDelay(float delay, string questId)
    {
        yield return new WaitForSeconds(delay);
        var questItem = _currentQuestsContainer.Q<VisualElement>(questId);
        if (questItem != null)
        {
            _currentQuestsContainer.Remove(questItem);
            logger.Log($"Removed quest item with id {questId} after delay.", this);
        }

        if (_currentQuestsContainer.childCount == 0)
            ToggleContainerVisibility(false);
    }
}
