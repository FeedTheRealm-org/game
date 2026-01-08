using System;
using System.Collections;
using Game.Core.Dialogue;
using Game.Core.Events;
using UnityEngine;

/// <summary>
/// Manages dialog sequences and notifies listeners of dialog changes.
/// </summary>
public class DialogManagerComponent : MonoBehaviour
{
    public event Action<MessageData> OnDialogChanged;
    public event Action<bool> OnToggleDialog;

    [Header("Dialog settings")]
    [SerializeField]
    private bool shouldResetOnComplete = true;

    [SerializeField]
    private float inactivityTimeout = 5f;

    [Header("NPC settings")]
    [SerializeField]
    private string npcName;

    [SerializeField]
    private NpcMessageData[] dialogs;
    private int _currentDialogIndex;

    [Header("General settings")]
    [SerializeField]
    private QuestOfferedEvent questOfferedEvent;

    [SerializeField]
    private Logging.Logger logger;

    private Coroutine _timeoutCoroutine;

    private bool _questOffered;

    private void Start()
    {
        _currentDialogIndex = 0;
        if (string.IsNullOrEmpty(npcName))
        {
            npcName = "???";
        }
    }

    private void OnDisable()
    {
        if (_timeoutCoroutine != null)
        {
            StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = null;
        }
    }

    /// <summary>
    /// Sets the dialog messages.
    /// </summary>
    public void SetDialogs(NpcMessageData[] dialogs)
    {
        this.dialogs = dialogs;
    }

    /// <summary>
    /// Advances to the next dialog message.
    /// </summary>
    public bool Next()
    {
        if (dialogs == null || _currentDialogIndex >= dialogs.Length)
        {
            CloseDialog(); // Dialog ended
            return false;
        }

        var dialog = dialogs[_currentDialogIndex];
        dialog.Msg.Sender = npcName;
        OnDialogChanged?.Invoke(dialog.Msg);

        if (_currentDialogIndex == 0)
            OpenDialog(); // Dialog started
        _currentDialogIndex++;

        var hasQuest = dialog.Quest != null && !string.IsNullOrEmpty(dialog.Quest.Id);
        _questOffered = hasQuest;
        if (hasQuest)
        {
            logger.Log(
                $"NPC '{npcName}' is offering quest '{dialog.Quest.Title}' (ID: {dialog.Quest.Id}).",
                this,
                Logging.LogType.Info
            );
            OnDisable();
            questOfferedEvent.Raise(dialog.Quest);
            return true;
        }

        RestartTimeout();

        return true;
    }

    public bool IsQuestOffer()
    {
        return _questOffered;
    }

    private void CloseDialog()
    {
        OnToggleDialog?.Invoke(false);
        if (shouldResetOnComplete)
            _currentDialogIndex = 0;
        if (_timeoutCoroutine != null)
        {
            StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = null;
        }
    }

    private void OpenDialog()
    {
        OnToggleDialog?.Invoke(true);
    }

    private void RestartTimeout()
    {
        if (_timeoutCoroutine != null)
            StopCoroutine(_timeoutCoroutine);

        _timeoutCoroutine = StartCoroutine(TimeoutCoroutine());
    }

    /// <summary>
    /// Coroutine to handle inactivity timeout.
    /// </summary>
    private IEnumerator TimeoutCoroutine()
    {
        yield return new WaitForSeconds(inactivityTimeout);
        CloseDialog();
    }
}
