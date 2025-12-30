using System;
using System.Collections;
using Game.Core.Dialogue;
using UnityEngine;

/// <summary>
/// Manages dialog sequences and notifies listeners of dialog changes.
/// </summary>
public class DialogManagerComponent : MonoBehaviour
{
    public event Action<Message> OnDialogChanged;
    public event Action<bool> OnToggleDialog;

    [SerializeField]
    private bool shouldResetOnComplete = true;

    [SerializeField]
    private float inactivityTimeout = 5f;

    [SerializeField]
    private Message[] _dialogs;
    private int _currentDialogIndex;

    private Coroutine _timeoutCoroutine;

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
    public void SetDialogs(Message[] dialogs)
    {
        _dialogs = dialogs;
    }

    /// <summary>
    /// Advances to the next dialog message.
    /// </summary>
    public bool Next()
    {
        if (_dialogs == null || _currentDialogIndex >= _dialogs.Length)
        {
            CloseDialog(); // Dialog ended
            return false;
        }

        var dialog = _dialogs[_currentDialogIndex];
        OnDialogChanged?.Invoke(dialog);

        if (_currentDialogIndex == 0)
            OpenDialog(); // Dialog started
        _currentDialogIndex++;

        RestartTimeout();

        return true;
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
