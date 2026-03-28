using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

/// <summary>
/// Client-side view that lives on the local player and reacts to dialog state
/// changes synced from the server. Fires EventChannel SOs so the NPC's
/// UIDialogController can react without a direct reference.
/// </summary>
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
    private CharacterStateStorage stateStorage;
    private string _activeNpcId;

    public void Initialize(
        NetworkEventRouter eventRouter,
        NpcDialogRegistry dialogRegistry,
        CharacterStateStorage stateStorage
    )
    {
        this.eventRouter = eventRouter;
        this.dialogRegistry = dialogRegistry;
        this.stateStorage = stateStorage;

        stateStorage.OnIsInteractingChanged += HandleIsInteractingChanged;
        stateStorage.OnCurrentNpcIdChanged += HandleNpcSwitched;
        eventRouter.OnDialogEvent += HandleDialogEvent;
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
            stateStorage.OnIsInteractingChanged -= HandleIsInteractingChanged;
        stateStorage.OnCurrentNpcIdChanged -= HandleNpcSwitched;
        if (eventRouter != null)
            eventRouter.OnDialogEvent -= HandleDialogEvent;
    }

    private void HandleIsInteractingChanged(bool isInteracting)
    {
        if (isInteracting)
        {
            _activeNpcId = stateStorage.CurrentNpcId;
            ShowDialogLine(_activeNpcId, stateStorage.CurrentDialogIndex);
            npcDialogToggledEvent.Raise((true, _activeNpcId));
        }
        else
        {
            npcDialogToggledEvent.Raise((false, _activeNpcId));
            npcDialogClosedEvent.Raise();
            _activeNpcId = null;
        }
    }

    private void HandleDialogEvent(DialogEventContent content)
    {
        if (content.DialogState == DialogStateType.DialogTypeAdvanced)
        {
            _activeNpcId = content.NpcId;
            ShowDialogLine(content.NpcId, content.DialogIndex);
        }
    }

    private void ShowDialogLine(string npcId, int index)
    {
        if (TryGetMessage(npcId, index, out MessageData message))
            npcDialogMessageEvent.Raise((npcId, message));
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
        return true;
    }

    private void HandleNpcSwitched(string newNpcId)
    {
        if (string.IsNullOrEmpty(newNpcId))
            return;

        if (!string.IsNullOrEmpty(_activeNpcId))
            npcDialogToggledEvent.Raise((false, _activeNpcId));

        _activeNpcId = newNpcId;
        ShowDialogLine(_activeNpcId, stateStorage.CurrentDialogIndex);
        npcDialogToggledEvent.Raise((true, _activeNpcId));
    }
}
