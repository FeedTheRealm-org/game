using System.Collections;
using Game.Core.Common.Events;
using Game.Core.Common.Quests;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controls the UI popup for the quest completion panel.
/// </summary>
public class QuestCompletionPanelController : MonoBehaviour
{
    [Header("General settings")]
    [SerializeField]
    private float hideDelay = 3f;

    [SerializeField]
    private Logging.Logger logger;

    private VisualElement _root;

    private Label _titleLabel;

    private Coroutine _hideCoroutine;

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _titleLabel = _root.Q<Label>("QuestTitle");
        if (_titleLabel == null)
            logger.Log(
                "One or more UI elements are not assigned in the inspector.",
                this,
                Logging.LogType.Error
            );
        ToggleQuestCompletionPanel(false);
    }

    private void OnDestroy()
    {
        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);
    }

    /// <summary>
    /// Toggles the visibility of the quest completion panel.
    /// </summary>
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

    /// <summary>
    /// Updates the quest completion panel with the completed quest data.
    /// </summary>
    public void OnQuestCompleted(QuestData data)
    {
        _titleLabel.text = data.Title;
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
