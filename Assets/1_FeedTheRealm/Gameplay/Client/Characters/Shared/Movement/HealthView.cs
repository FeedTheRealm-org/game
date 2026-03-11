using FTR.Core.Client.EventChannels.Status;
using FTR.Core.Common.Systems.Status;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

/// <summary>
/// Propagates health changes from the CharacterStateStorage SyncVar to the
/// HealthChangedEvent channel (HUD and world-space bars).
/// Animation reactions (damaged / death) are handled by HitView.
/// </summary>
public class HealthView : MonoBehaviour
{
    [Inject]
    private HealthChangedEvent healthChangedEvent;

    /// <summary>
    /// Maximum health for this character. Should match the server-side HealthSystem.MaxHealth.
    /// </summary>
    [SerializeField]
    private float maxHealth = 100f;

    private CharacterStateStorage stateStorage;

    public void Initialize(CharacterStateStorage stateStorage)
    {
        this.stateStorage = stateStorage;
        stateStorage.OnHealthChanged += OnHealthChanged;
        RaiseEvent(stateStorage.Health);
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
            stateStorage.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float value)
    {
        RaiseEvent(value);
    }

    private void RaiseEvent(float currentHealth)
    {
        healthChangedEvent?.Raise(
            new HealthChangedData(stateStorage.netId, currentHealth, maxHealth)
        );
    }
}
