using System.Collections;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Quests;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.Hud.Main
{
    /// <summary>
    /// Manages the quest progress panel in the HUD.
    /// Lives on the Hud prefab.
    /// </summary>
    public class QuestProgressPanelController : MonoBehaviour
    {
        [Inject]
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

        private void Start()
        {
            var rootDocument = GetComponent<UIDocument>();
            if (rootDocument == null)
            {
                logger?.Log(
                    "[QuestProgressPanel] UIDocument component missing.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            var root = rootDocument.rootVisualElement;

            _currentQuestsContainer = root.Q<ScrollView>("CurrentQuestsContainer");
            if (_currentQuestsContainer == null)
            {
                StartCoroutine(DeferredInitializeContainer());
                return;
            }

            _currentQuestsContainer.Clear();
            ToggleContainerVisibility(false);
        }

        private IEnumerator DeferredInitializeContainer()
        {
            yield return null;
            var root = GetComponent<UIDocument>().rootVisualElement;
            _currentQuestsContainer = root.Q<ScrollView>("CurrentQuestsContainer");
            if (_currentQuestsContainer == null)
            {
                logger?.Log(
                    "[QuestProgressPanel] CurrentQuestsContainer not found in UIDocument even after delay.",
                    this,
                    Logging.LogType.Error
                );
                yield break;
            }
            _currentQuestsContainer.Clear();
            ToggleContainerVisibility(false);
        }

        private void OnEnable()
        {
            if (progressEvent != null)
                progressEvent.OnRaised += HandleQuestProgress;
        }

        private void OnDisable()
        {
            if (progressEvent != null)
                progressEvent.OnRaised -= HandleQuestProgress;
        }

        private void HandleQuestProgress(QuestProgressData questProgress)
        {
            /*logger?.Log(
                $"[QuestProgressPanel] Progress: '{questProgress.Quest.title}' "
                    + $"{questProgress.CurrentProgressAmount}/{questProgress.TargetProgressAmount}",
                this
            );*/

            var questItem = _currentQuestsContainer.Q<VisualElement>(questProgress.Id);

            if (questItem == null)
            {
                questItem = CreateQuestItem(questProgress.Quest);
                ToggleContainerVisibility(true);
            }

            var progressBar = questItem.Q<ProgressBar>();
            if (progressBar == null)
            {
                logger?.Log(
                    $"[QuestProgressPanel] ProgressBar not found for quest '{questProgress.Id}'.",
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
                StartCoroutine(
                    RemoveQuestAfterDelay(completeQuestDisplayDuration, questProgress.Id)
                );
        }

        private VisualElement CreateQuestItem(QuestData questData)
        {
            var questItem = new VisualElement { name = questData.id };
            questItem.AddToClassList(_questItemClasses);

            var titleLabel = new Label { text = questData.title };
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
                $"Created new quest item for quest {questData.title} with id {questData.id}",
                this
            );

            return questItem;
        }

        private void ToggleContainerVisibility(bool show)
        {
            if (_currentQuestsContainer == null)
                return;

            _currentQuestsContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private IEnumerator RemoveQuestAfterDelay(float delay, string questId)
        {
            yield return new WaitForSeconds(delay);
            var questItem = _currentQuestsContainer?.Q<VisualElement>(questId);
            if (questItem != null)
            {
                _currentQuestsContainer.Remove(questItem);
                logger?.Log(
                    $"[QuestProgressPanel] Removed quest item '{questId}' after delay.",
                    this
                );
            }

            if (_currentQuestsContainer != null && _currentQuestsContainer.childCount == 0)
                ToggleContainerVisibility(false);
        }
    }
}
