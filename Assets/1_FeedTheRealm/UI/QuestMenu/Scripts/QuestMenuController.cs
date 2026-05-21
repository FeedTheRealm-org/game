using System.Collections;
using System.Collections.Generic;
using Enums;
using FTR.Core.Client.Managers;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Quests;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.Hud.QuestMenu
{
    /// <summary>
    /// Controls the full-screen Quest Journal panel.
    /// Displays all active quests in a sidebar; clicking a quest reveals
    /// detailed information including title, description, objective and progress.
    /// </summary>
    public class QuestMenuController : MonoBehaviour
    {
        [Inject]
        private QuestProgressEvent progressEvent;

        [Inject]
        private QuestMenuToggleEvent toggleEvent;

        [Inject]
        private NpcDialogRegistry npcDialogRegistry;

        [Inject]
        private MenuManager menuManager;

        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private float completeQuestRemoveDelay = 5f;

        [SerializeField]
        private bool autoRemoveCompletedQuests = false;

        private UIDocument _uiDocument;
        private VisualElement _overlay;
        private ScrollView _questListScroll;
        private VisualElement _detailContent;
        private Label _emptyStateLabel;
        private Label _detailTitle;
        private Label _detailDescription;
        private Label _detailObjective;
        private ProgressBar _detailProgress;
        private VisualElement _detailIndicator;

        private readonly Dictionary<string, QuestProgressData> _questDataMap = new();
        private readonly Dictionary<string, VisualElement> _questListItems = new();
        private string _selectedQuestId;

        // Style class names
        private const string QuestListItemClass = "quest-list-item";
        private const string QuestListItemSelectedClass = "quest-list-item--selected";
        private const string QuestListItemCompletedClass = "quest-list-item--completed";
        private const string QuestListItemIndicatorClass = "quest-list-item__indicator";
        private const string QuestCompletedClass = "quest-completed";
        private const float QuestProgressHighValue = 100f;

        private bool _isVisible;

        private void Start()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                logger?.Log(
                    "[QuestMenu] UIDocument component missing.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            var root = _uiDocument.rootVisualElement;

            _overlay = root.Q<VisualElement>("QuestMenuOverlay");
            _questListScroll = root.Q<ScrollView>("QuestListScroll");
            _detailContent = root.Q<VisualElement>("DetailContent");
            _emptyStateLabel = root.Q<Label>("EmptyStateLabel");
            _detailTitle = root.Q<Label>("DetailTitle");
            _detailDescription = root.Q<Label>("DetailDescription");
            _detailObjective = root.Q<Label>("DetailObjective");
            _detailProgress = root.Q<ProgressBar>("DetailProgress");
            _detailIndicator = root.Q<VisualElement>("DetailIndicator");

            if (_overlay != null)
                _overlay.style.display = DisplayStyle.None;

            ToggleEmptyState(true);
        }

        private void OnEnable()
        {
            if (progressEvent != null)
                progressEvent.OnRaised += HandleQuestProgress;

            if (toggleEvent != null)
                toggleEvent.OnRaised += OnToggleRequested;
        }

        private void OnDisable()
        {
            if (progressEvent != null)
                progressEvent.OnRaised -= HandleQuestProgress;

            if (toggleEvent != null)
                toggleEvent.OnRaised -= OnToggleRequested;
        }

        /* ═══════════════════════════════════════════════════════════
           PUBLIC API
           ═══════════════════════════════════════════════════════════ */

        public void TogglePanel()
        {
            if (_isVisible)
                HidePanel();
            else
                ShowPanel();
        }

        public void ShowPanel()
        {
            Debug.Log("Showing quest menu");
            if (_overlay == null)
                return;

            if (!menuManager.CanOpenMenu(MenuType.Quests))
                return;

            _overlay.style.display = DisplayStyle.Flex;
            _isVisible = true;

            menuManager.ToggleMenu(MenuType.Quests, true);

            if (_selectedQuestId == null && _questDataMap.Count > 0)
            {
                foreach (var key in _questDataMap.Keys)
                {
                    SelectQuest(key);
                    break;
                }
            }
        }

        public void HidePanel()
        {
            if (_overlay == null)
                return;

            _overlay.style.display = DisplayStyle.None;
            _isVisible = false;
            menuManager.ToggleMenu(MenuType.Quests, false);
        }

        /* ═══════════════════════════════════════════════════════════
           QUEST PROGRESS HANDLING
           ═══════════════════════════════════════════════════════════ */

        private void HandleQuestProgress(QuestProgressData questProgress)
        {
            bool isNewQuest = !_questDataMap.ContainsKey(questProgress.Id);

            // Store latest data
            _questDataMap[questProgress.Id] = questProgress;

            if (isNewQuest)
            {
                CreateQuestListItem(questProgress);
            }
            else
            {
                UpdateQuestListItem(questProgress);
            }

            if (_selectedQuestId == questProgress.Id)
            {
                ShowQuestDetails(questProgress);
            }

            if (questProgress.CurrentProgressAmount >= questProgress.TargetProgressAmount)
            {
                if (autoRemoveCompletedQuests && completeQuestRemoveDelay > 0)
                {
                    StartCoroutine(
                        RemoveQuestAfterDelay(completeQuestRemoveDelay, questProgress.Id)
                    );
                }
            }
        }

        private void OnToggleRequested()
        {
            TogglePanel();
        }

        /* ═══════════════════════════════════════════════════════════
           LIST ITEM MANAGEMENT
           ═══════════════════════════════════════════════════════════ */

        private void CreateQuestListItem(QuestProgressData questProgress)
        {
            var item = new VisualElement { name = $"list-item-{questProgress.Id}" };
            item.AddToClassList(QuestListItemClass);

            var indicator = new VisualElement();
            indicator.AddToClassList(QuestListItemIndicatorClass);
            item.Add(indicator);

            var label = new Label(questProgress.Quest.title) { name = "ListItemTitle" };
            label.style.flexGrow = 1;
            item.Add(label);

            item.RegisterCallback<ClickEvent>(_ => SelectQuest(questProgress.Id));

            _questListItems[questProgress.Id] = item;
            _questListScroll.Add(item);

            if (_selectedQuestId == null && _isVisible)
            {
                SelectQuest(questProgress.Id);
            }
        }

        private void UpdateQuestListItem(QuestProgressData questProgress)
        {
            if (!_questListItems.TryGetValue(questProgress.Id, out var item))
                return;

            bool isCompleted =
                questProgress.TargetProgressAmount > 0
                && questProgress.CurrentProgressAmount >= questProgress.TargetProgressAmount;

            if (isCompleted)
            {
                item.AddToClassList(QuestListItemCompletedClass);
            }
        }

        private void SelectQuest(string questId)
        {
            if (
                _selectedQuestId != null
                && _questListItems.TryGetValue(_selectedQuestId, out var prevItem)
            )
            {
                prevItem.RemoveFromClassList(QuestListItemSelectedClass);
            }

            _selectedQuestId = questId;

            if (_questListItems.TryGetValue(questId, out var newItem))
            {
                newItem.AddToClassList(QuestListItemSelectedClass);
                _questListScroll.ScrollTo(newItem);
            }

            if (_questDataMap.TryGetValue(questId, out var data))
            {
                ShowQuestDetails(data);
            }
        }

        /* ═══════════════════════════════════════════════════════════
           DETAIL PANEL
           ═══════════════════════════════════════════════════════════ */

        private void ShowQuestDetails(QuestProgressData questProgress)
        {
            ToggleEmptyState(false);

            var quest = questProgress.Quest;

            _detailTitle.text = quest.title;
            _detailDescription.text = quest.content;

            string objectiveText = GetQuestTargetDescription(quest);
            if (questProgress.TargetProgressAmount > 0)
            {
                objectiveText +=
                    $"  —  {questProgress.CurrentProgressAmount}/{questProgress.TargetProgressAmount}";
            }
            _detailObjective.text = objectiveText;

            float percentComplete = 0f;
            if (questProgress.TargetProgressAmount > 0)
            {
                percentComplete =
                    (
                        (float)questProgress.CurrentProgressAmount
                        / questProgress.TargetProgressAmount
                    ) * QuestProgressHighValue;
            }
            percentComplete = Mathf.Clamp(percentComplete, 0f, QuestProgressHighValue);
            _detailProgress.value = percentComplete;
            _detailProgress.title = $"{Mathf.Round(percentComplete):F0}%";

            bool isCompleted =
                questProgress.TargetProgressAmount > 0
                && questProgress.CurrentProgressAmount >= questProgress.TargetProgressAmount;

            if (isCompleted)
            {
                _detailContent.AddToClassList(QuestCompletedClass);
            }
            else
            {
                _detailContent.RemoveFromClassList(QuestCompletedClass);
            }
        }

        private void ToggleEmptyState(bool show)
        {
            if (_emptyStateLabel != null)
                _emptyStateLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (_detailContent != null)
                _detailContent.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /* ═══════════════════════════════════════════════════════════
           QUEST DATA HELPERS
           ═══════════════════════════════════════════════════════════ */

        private string GetQuestTargetDescription(QuestData questData)
        {
            switch (questData.type)
            {
                case QuestType.EnemySlays:
                {
                    var enemy = ClientItemsRegistry.GetEnemyById(questData.targetId);
                    string enemyName =
                        enemy != null && !string.IsNullOrEmpty(enemy.name)
                            ? enemy.name
                            : questData.targetId;
                    return $"Slay {enemyName}";
                }

                case QuestType.NpcInteract:
                {
                    string npcId = string.IsNullOrEmpty(questData.targetId)
                        ? questData.targetInteractionId
                        : questData.targetId;
                    return $"Talk with {GetNpcName(npcId)}";
                }

                default:
                    return questData.content ?? questData.targetId;
            }
        }

        private string GetNpcName(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
                return "Unknown NPC";

            if (
                npcDialogRegistry != null
                && npcDialogRegistry.TryGetNpcName(npcId, out string npcName)
            )
            {
                return string.IsNullOrEmpty(npcName) ? npcId : npcName;
            }

            return npcId;
        }

        /* ═══════════════════════════════════════════════════════════
           CLEANUP
           ═══════════════════════════════════════════════════════════ */

        private IEnumerator RemoveQuestAfterDelay(float delay, string questId)
        {
            yield return new WaitForSeconds(delay);

            _questDataMap.Remove(questId);

            if (_questListItems.TryGetValue(questId, out var item))
            {
                _questListScroll.Remove(item);
                _questListItems.Remove(questId);
            }

            if (_selectedQuestId == questId)
            {
                _selectedQuestId = null;

                foreach (var key in _questListItems.Keys)
                {
                    SelectQuest(key);
                    yield break;
                }

                ToggleEmptyState(true);
            }
        }
    }
}
