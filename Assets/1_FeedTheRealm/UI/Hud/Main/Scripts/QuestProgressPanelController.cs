using System.Collections;
using Enums;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Quests;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.Environment.Dialogs;
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

        [Inject]
        private NpcDialogRegistry npcDialogRegistry;

        private ScrollView _currentQuestsContainer;

        private readonly string _questItemClasses = "quest-item";
        private readonly string _questItemCompletedClass = "quest-item--completed";
        private readonly string _questDetailClasses = "quest-detail";

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
            var questItem = _currentQuestsContainer.Q<VisualElement>(questProgress.Id);

            if (questItem == null)
            {
                questItem = CreateQuestItem(questProgress);
                ToggleContainerVisibility(true);
            }

            var detailLabel = questItem.Q<Label>("QuestProgressDetail");
            if (detailLabel != null)
            {
                detailLabel.text = GetQuestProgressText(
                    questProgress.Quest,
                    questProgress.CurrentProgressAmount,
                    questProgress.TargetProgressAmount
                );
            }

            if (questProgress.CurrentProgressAmount >= questProgress.TargetProgressAmount)
            {
                questItem.AddToClassList(_questItemCompletedClass);
                StartCoroutine(
                    RemoveQuestAfterDelay(completeQuestDisplayDuration, questProgress.Id)
                );
            }
        }

        private VisualElement CreateQuestItem(QuestProgressData questProgress)
        {
            var questItem = new VisualElement { name = questProgress.Id };
            questItem.AddToClassList(_questItemClasses);

            var icon = new VisualElement();
            icon.AddToClassList("quest-item-icon");
            if (questProgress.Quest.type == QuestType.EnemySlays)
                icon.AddToClassList("quest-icon-slay");
            else if (questProgress.Quest.type == QuestType.NpcInteract)
                icon.AddToClassList("quest-icon-interact");

            questItem.Add(icon);

            var detailLabel = new Label
            {
                name = "QuestProgressDetail",
                text = GetQuestProgressText(
                    questProgress.Quest,
                    0,
                    questProgress.TargetProgressAmount
                ),
            };
            detailLabel.AddToClassList(_questDetailClasses);
            questItem.Add(detailLabel);

            _currentQuestsContainer.Add(questItem);

            return questItem;
        }

        private string GetQuestProgressText(
            QuestData questData,
            int currentProgress,
            int targetProgress
        )
        {
            var targetLabel = GetQuestTargetDescription(questData);
            if (questData.type == QuestType.NpcInteract)
                return targetLabel;
            return targetProgress > 0
                ? $"{currentProgress}/{targetProgress} {targetLabel}"
                : targetLabel;
        }

        private string GetQuestTargetDescription(QuestData questData)
        {
            switch (questData.type)
            {
                case QuestType.EnemySlays:
                {
                    var enemy = ClientItemsRegistry.GetEnemyById(questData.targetId);
                    return $"{(enemy != null && !string.IsNullOrEmpty(enemy.name) ? enemy.name : questData.targetId)} Slain";
                }
                case QuestType.NpcInteract:
                {
                    var npcId = string.IsNullOrEmpty(questData.targetId)
                        ? questData.targetInteractionId
                        : questData.targetId;
                    return $"Meet with {GetNpcName(npcId)}";
                }
                default:
                    return questData.content ?? questData.targetId;
            }
        }

        private string GetNpcName(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
                return "Unknown NPC";

            if (npcDialogRegistry != null)
            {
                if (npcDialogRegistry.TryGetNpcName(npcId, out string npcName))
                {
                    return string.IsNullOrEmpty(npcName) ? npcId : npcName;
                }
            }

            return npcId;
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
            }

            if (_currentQuestsContainer != null && _currentQuestsContainer.childCount == 0)
                ToggleContainerVisibility(false);
        }
    }
}
