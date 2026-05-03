using FTR.Core.Client.EventChannels.Interaction;
using FTR.Core.Client.EventChannels.Quest;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

public class InteractView : MonoBehaviour
{
    [Inject]
    private NpcDialogClosedEvent npcDialogClosedEvent;

    [Inject]
    private NpcDialogMessageEvent npcDialogMessageEvent;

    [Inject]
    private NpcDialogToggledEvent npcDialogToggledEvent;

    [Inject]
    private NpcQuestOfferedEvent npcQuestOfferedEvent;

    [Inject]
    private InteractFailedEvent interactFailedEvent;

    [Inject]
    private InteractCompletedEvent interactCompletedEvent;

    private NetworkEventRouter eventRouter;
    private NpcDialogRegistry dialogRegistry;
    private string _activeNpcId;

    public void Initialize(NetworkEventRouter eventRouter, NpcDialogRegistry dialogRegistry)
    {
        this.eventRouter = eventRouter;
        this.dialogRegistry = dialogRegistry;

        eventRouter.OnDialogEvent += HandleDialogEvent;
        eventRouter.OnInteractFailedEvent += HandleInteractFailed;
        eventRouter.OnInteractCompletedEvent += HandleInteractCompleted;

        Debug.Log(
            $"[InteractView] Initialized. eventRouter={eventRouter != null}, dialogRegistry={dialogRegistry != null}"
        );
    }

    private void OnDestroy()
    {
        if (eventRouter == null)
            return;

        eventRouter.OnDialogEvent -= HandleDialogEvent;
        eventRouter.OnInteractFailedEvent -= HandleInteractFailed;
        eventRouter.OnInteractCompletedEvent -= HandleInteractCompleted;
    }

    private void HandleInteractFailed()
    {
        Debug.Log("[InteractView] HandleInteractFailed.");
        interactFailedEvent.Raise();
    }

    private void HandleInteractCompleted()
    {
        Debug.Log("[InteractView] HandleInteractCompleted.");
        interactCompletedEvent.Raise();
    }

    private void HandleDialogEvent(DialogEventContent content)
    {
        Debug.Log(
            $"[InteractView] HandleDialogEvent. NpcId={content.NpcId}, State={content.DialogState}, DialogId={content.DialogId}, Index={content.DialogIndex}, QuestId={content.QuestId}"
        );

        switch (content.DialogState)
        {
            case DialogStateType.DialogTypeStarted:
                if (!string.IsNullOrEmpty(_activeNpcId) && _activeNpcId != content.NpcId)
                    npcDialogToggledEvent.Raise((false, _activeNpcId));

                _activeNpcId = content.NpcId;
                npcDialogToggledEvent.Raise((true, _activeNpcId));
                ShowDialogLine(content.NpcId, content.DialogId, content.DialogIndex);

                if (!string.IsNullOrEmpty(content.QuestId))
                    npcQuestOfferedEvent.Raise((content.QuestId, content.NpcId));
                break;

            case DialogStateType.DialogTypeAdvanced:
                if (_activeNpcId != content.NpcId)
                {
                    if (!string.IsNullOrEmpty(_activeNpcId))
                        npcDialogToggledEvent.Raise((false, _activeNpcId));
                    _activeNpcId = content.NpcId;
                    npcDialogToggledEvent.Raise((true, _activeNpcId));
                }

                ShowDialogLine(content.NpcId, content.DialogId, content.DialogIndex);

                if (!string.IsNullOrEmpty(content.QuestId))
                    npcQuestOfferedEvent.Raise((content.QuestId, content.NpcId));
                break;

            case DialogStateType.DialogTypeClosed:
                if (!string.IsNullOrEmpty(_activeNpcId))
                {
                    npcDialogToggledEvent.Raise((false, _activeNpcId));
                    npcDialogClosedEvent.Raise();
                    _activeNpcId = null;
                }
                break;
        }
    }

    /// <summary>
    /// Looks up the message by dialogId + index.
    /// </summary>
    private void ShowDialogLine(string npcId, string dialogId, int index)
    {
        if (TryGetMessage(npcId, dialogId, index, out var message))
        {
            Debug.Log(
                $"[InteractView] ShowDialogLine -> NpcId={npcId}, DialogId={dialogId}, Index={index}, Sender={message.sender}"
            );
            npcDialogMessageEvent.Raise((npcId, message));
        }
    }

    private bool TryGetMessage(string npcId, string dialogId, int index, out MessageData message)
    {
        message = default;

        if (!string.IsNullOrEmpty(dialogId))
        {
            if (!dialogRegistry.TryGetMessagesByDialogId(dialogId, out var byDialog))
            {
                Debug.LogWarning($"[InteractView] No messages for DialogId '{dialogId}'.");
                return false;
            }

            if (index < 0 || index >= byDialog.Count)
            {
                Debug.LogWarning(
                    $"[InteractView] Index {index} out of range for DialogId '{dialogId}' (count: {byDialog.Count})."
                );
                return false;
            }

            message = byDialog[index];

            if (string.IsNullOrEmpty(message.sender) && !string.IsNullOrEmpty(npcId))
            {
                dialogRegistry.TryGetNpcName(npcId, out var npcName);
                message.sender = npcName;
            }

            return true;
        }

        Debug.LogWarning(
            $"[InteractView] DialogId is empty for NpcId='{npcId}', Index={index} — cannot resolve message."
        );
        return false;
    }
}
