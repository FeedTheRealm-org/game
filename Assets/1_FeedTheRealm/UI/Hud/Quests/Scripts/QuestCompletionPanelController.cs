using System.Collections;
using FTR.Core.Common.EventChannels;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// Controls the UI popup for the quest completion panel.
/// </summary>
public class QuestCompletionPanelController : MonoBehaviour
{
    [Header("General settings")]
    [SerializeField]
    private float hideDelay = 6f;

    [SerializeField]
    private Logging.Logger logger;

    [Inject]
    private QuestCompletedEvent completedEvent;

    private VisualElement _root;

    private Label _titleLabel;

    private Coroutine _hideCoroutine;

    private void Awake()
    {
        var document = GetComponent<UIDocument>();
        if (document == null)
        {
            logger.Log("UIDocument component missing.", this, Logging.LogType.Error);
            return;
        }

        _root = document.rootVisualElement;
        _titleLabel = _root?.Q<Label>("QuestTitle");
        if (_root == null || _titleLabel == null)
        {
            logger.Log(
                "One or more UI elements are not assigned in the inspector or UI document is incomplete.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        ToggleQuestCompletionPanel(false);
    }

    private void OnEnable()
    {
        if (completedEvent != null)
            completedEvent.OnRaised += HandleQuestCompleted;
    }

    private void OnDisable()
    {
        if (completedEvent != null)
            completedEvent.OnRaised -= HandleQuestCompleted;
    }

    private void OnDestroy()
    {
        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);
    }

    public void ToggleQuestCompletionPanel(bool show)
    {
        _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (show)
        {
            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideAfterDelay(hideDelay));
        }
    }

    private void HandleQuestCompleted((QuestData Quest, string EffectiveId) payload)
    {
        OnQuestCompleted(payload.Quest);
        ToggleQuestCompletionPanel(true);
    }

    public void OnQuestCompleted(QuestData data)
    {
        _titleLabel.text = data.title;
    }

    /// <summary>
    /// Hides the quest completion panel after a specified delay.
    /// </summary>
    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ToggleQuestCompletionPanel(false);
    }
}
