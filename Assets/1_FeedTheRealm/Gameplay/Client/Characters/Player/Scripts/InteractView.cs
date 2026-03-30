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

    private NetworkEventRouter eventRouter;
    private NpcDialogRegistry dialogRegistry;
    private string _activeNpcId;

    public void Initialize(NetworkEventRouter eventRouter, NpcDialogRegistry dialogRegistry)
    {
        this.eventRouter = eventRouter;
        this.dialogRegistry = dialogRegistry;

        eventRouter.OnDialogEvent += HandleDialogEvent;
        Debug.Log(
            $"[InteractView] Initialized. eventRouter set: {eventRouter != null}, dialogRegistry set: {dialogRegistry != null}"
        );
    }

    private void OnDestroy()
    {
        if (eventRouter != null)
            eventRouter.OnDialogEvent -= HandleDialogEvent;
    }

    private void HandleDialogEvent(DialogEventContent content)
    {
        Debug.Log(
            $"[InteractView] HandleDialogEvent received. NpcId={content.NpcId}, DialogState={content.DialogState}, DialogIndex={content.DialogIndex}"
        );
        if (content.DialogState == DialogStateType.DialogTypeStarted)
        {
            if (!string.IsNullOrEmpty(_activeNpcId))
            {
                npcDialogToggledEvent.Raise((false, _activeNpcId));
            }
            _activeNpcId = content.NpcId;
            npcDialogToggledEvent.Raise((true, _activeNpcId));
            ShowDialogLine(content.NpcId, content.DialogIndex);
        }
        else if (content.DialogState == DialogStateType.DialogTypeAdvanced)
        {
            if (_activeNpcId != content.NpcId)
            {
                if (!string.IsNullOrEmpty(_activeNpcId))
                    npcDialogToggledEvent.Raise((false, _activeNpcId));
                _activeNpcId = content.NpcId;
                npcDialogToggledEvent.Raise((true, _activeNpcId));
            }
            ShowDialogLine(content.NpcId, content.DialogIndex);
        }
        else if (content.DialogState == DialogStateType.DialogTypeClosed)
        {
            if (!string.IsNullOrEmpty(_activeNpcId))
            {
                npcDialogToggledEvent.Raise((false, _activeNpcId));
                npcDialogClosedEvent.Raise();
                _activeNpcId = null;
            }
        }
    }

    private void ShowDialogLine(string npcId, int index)
    {
        if (TryGetMessage(npcId, index, out MessageData message))
        {
            Debug.Log(
                $"[InteractView] ShowDialogLine -> raising message for NpcId={npcId}, Index={index}, Sender={message.Sender}, Content={message.Content}"
            );
            npcDialogMessageEvent.Raise((npcId, message));
        }
    }

    private bool TryGetMessage(string npcId, int index, out MessageData message)
    {
        message = default;

        if (!dialogRegistry.TryGetMessages(npcId, out var messages))
        {
            Debug.LogWarning($"[InteractView] No messages found for NpcId '{npcId}'.");
            return false;
        }

        if (index < 0 || index >= messages.Count)
        {
            Debug.LogWarning(
                $"[InteractView] Index {index} out of range for '{npcId}' (count: {messages.Count})."
            );
            return false;
        }

        message = messages[index];
        Debug.Log(
            $"[InteractView] TryGetMessage -> found message for NpcId={npcId}, Index={index}"
        );
        return true;
    }
}
