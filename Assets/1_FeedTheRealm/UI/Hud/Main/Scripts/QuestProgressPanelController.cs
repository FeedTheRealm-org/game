using System;
using System.Collections;
using Enums;
using FTR.Core.Client.EventChannels.Quest;
using FTR.Core.Client.Managers;
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

        [Inject]
        private QuestTrackToggleEvent questTrackToggleEvent;

        [Inject]
        private MenuManager menuManager;

        [SerializeField]
        private float completeQuestDisplayDuration = 3f;

        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        private NpcDialogRegistry npcDialogRegistry;

        private ScrollView _currentQuestsContainer;
        private Label _chatBox;

        private readonly string _questItemClasses = "quest-item";
        private readonly string _questItemCompletedClass = "quest-item--completed";
        private readonly string _questDetailClasses = "quest-detail";
        private readonly string _hiddenClass = "panel--hidden";
        private readonly string _visibleClass = "panel--visible";

        private readonly System.Collections.Generic.Dictionary<
            string,
            QuestProgressData
        > _allQuests = new();
        private readonly System.Collections.Generic.HashSet<string> _trackedQuests = new();
        private const int MaxTrackedQuests = 4;

        private Coroutine _hideContainerCoroutine;
        private Coroutine _hideChatBoxCoroutine;

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
            _chatBox = root.Q<Label>("ChatBox");
            if (_chatBox != null)
                _chatBox.style.display = DisplayStyle.Flex;

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
            _chatBox = root.Q<Label>("ChatBox");
            if (_chatBox != null)
                _chatBox.style.display = DisplayStyle.Flex;

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
            if (questTrackToggleEvent != null)
                questTrackToggleEvent.OnRaised += OnQuestTrackToggled;
            if (menuManager != null)
                menuManager.OnMenuStatusChanged += HandleMenuStatusChanged;
        }

        private void OnDisable()
        {
            if (progressEvent != null)
                progressEvent.OnRaised -= HandleQuestProgress;
            if (questTrackToggleEvent != null)
                questTrackToggleEvent.OnRaised -= OnQuestTrackToggled;
            if (menuManager != null)
                menuManager.OnMenuStatusChanged -= HandleMenuStatusChanged;
        }

        private void HandleQuestProgress(QuestProgressData questProgress)
        {
            bool isNew = !_allQuests.ContainsKey(questProgress.Id);
            bool wasCompleted =
                !isNew
                && _allQuests[questProgress.Id].TargetProgressAmount > 0
                && _allQuests[questProgress.Id].CurrentProgressAmount
                    >= _allQuests[questProgress.Id].TargetProgressAmount;
            bool isCompleted =
                questProgress.TargetProgressAmount > 0
                && questProgress.CurrentProgressAmount >= questProgress.TargetProgressAmount;

            if (isNew && isCompleted)
            {
                _allQuests[questProgress.Id] = questProgress;
                return;
            }

            if ((isNew || wasCompleted) && !isCompleted && _trackedQuests.Count < MaxTrackedQuests)
            {
                _trackedQuests.Add(questProgress.Id);
            }
            _allQuests[questProgress.Id] = questProgress;

            if (!_trackedQuests.Contains(questProgress.Id))
                return;

            var questItem = _currentQuestsContainer?.Q<VisualElement>(questProgress.Id);

            if (questItem == null)
            {
                questItem = CreateQuestItem(questProgress);
                ToggleContainerVisibility(true);
            }
            else
            {
                questItem.RemoveFromClassList(_questItemCompletedClass);
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

        private void OnQuestTrackToggled(QuestTrackData data)
        {
            if (data.IsTracked)
            {
                if (_trackedQuests.Count < MaxTrackedQuests)
                {
                    _trackedQuests.Add(data.QuestId);
                    if (_allQuests.TryGetValue(data.QuestId, out var quest))
                    {
                        HandleQuestProgress(quest);
                    }
                }
            }
            else
            {
                _trackedQuests.Remove(data.QuestId);
                var questItem = _currentQuestsContainer?.Q<VisualElement>(data.QuestId);
                if (questItem != null)
                {
                    _currentQuestsContainer.Remove(questItem);
                    if (_currentQuestsContainer.childCount == 0)
                        ToggleContainerVisibility(false);
                }
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

            if (show)
            {
                _currentQuestsContainer.style.display = DisplayStyle.Flex;
                _currentQuestsContainer.schedule.Execute(() =>
                {
                    _currentQuestsContainer.RemoveFromClassList(_hiddenClass);
                    _currentQuestsContainer.AddToClassList(_visibleClass);
                });
            }
            else
            {
                _currentQuestsContainer.RemoveFromClassList(_visibleClass);
                _currentQuestsContainer.AddToClassList(_hiddenClass);

                if (_hideContainerCoroutine != null)
                    StopCoroutine(_hideContainerCoroutine);
                _hideContainerCoroutine = StartCoroutine(
                    HideAfterTransition(_currentQuestsContainer, 0.35f)
                );
            }
        }

        private void HandleMenuStatusChanged(MenuType type, bool isOpen)
        {
            if ((type == MenuType.Chat || type == MenuType.Inventory) && _chatBox != null)
            {
                if (isOpen)
                {
                    _chatBox.RemoveFromClassList(_visibleClass);
                    _chatBox.AddToClassList(_hiddenClass);

                    if (_hideChatBoxCoroutine != null)
                        StopCoroutine(_hideChatBoxCoroutine);
                    _hideChatBoxCoroutine = StartCoroutine(HideAfterTransition(_chatBox, 0.35f));
                }
                else
                {
                    _chatBox.style.display = DisplayStyle.Flex;
                    _chatBox.schedule.Execute(() =>
                    {
                        _chatBox.RemoveFromClassList(_hiddenClass);
                        _chatBox.AddToClassList(_visibleClass);
                    });
                }
            }

            if (type != MenuType.Inventory)
                return;

            var shouldShow =
                !isOpen
                && _currentQuestsContainer != null
                && _currentQuestsContainer.childCount > 0;
            ToggleContainerVisibility(shouldShow);
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

        private IEnumerator HideAfterTransition(VisualElement element, float transitionDuration)
        {
            yield return new WaitForSeconds(transitionDuration);
            if (element.ClassListContains(_hiddenClass))
                element.style.display = DisplayStyle.None;
        }
    }
}
