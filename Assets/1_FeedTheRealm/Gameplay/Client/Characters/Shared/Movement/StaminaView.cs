using System;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;

/// <summary>
/// Tracks the local player's stamina as a plain float and notifies the HUD via a static event.
/// </summary>
public class StaminaView : MonoBehaviour
{
    public static event Action<float> StaminaChangedEvent;

    private CharacterStateStorage stateStorage;

    public void Initialize(CharacterStateStorage stateStorage)
    {
        this.stateStorage = stateStorage;
        stateStorage.OnStaminaChanged += OnStaminaChanged;
        StaminaChangedEvent?.Invoke(stateStorage.Stamina);
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
            stateStorage.OnStaminaChanged -= OnStaminaChanged;
    }

    private void OnStaminaChanged(float value)
    {
        StaminaChangedEvent?.Invoke(value);
    }
}
