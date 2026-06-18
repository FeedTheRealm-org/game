using System;
using FTR.Core.Client.EventChannels.Status;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

/// <summary>
/// Tracks the local player's stamina as a plain float and notifies the HUD via a static event.
/// </summary>
public class StaminaView : MonoBehaviour
{
    [Inject]
    private StaminaChangedEvent staminaChangedEvent;

    private CharacterStateStorage stateStorage;

    public void Initialize(CharacterStateStorage stateStorage)
    {
        this.stateStorage = stateStorage;
        stateStorage.OnStaminaChanged += OnStaminaChanged;
        staminaChangedEvent.Raise(this.stateStorage.Stamina);
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
            stateStorage.OnStaminaChanged -= OnStaminaChanged;
    }

    private void OnStaminaChanged(float value)
    {
        if (!stateStorage.IsLocalPlayer)
            return;
        staminaChangedEvent.Raise(value);
    }
}
