using FTR.Core.Client.EventChannels.Status;
using FTR.Core.Common.Systems.Status;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

/// <summary>
/// Propagates health changes from CharacterStateStorage to the HealthChangedEvent
/// channel (local player HUD) and drives character animations for all characters.
/// </summary>
public class HealthView : MonoBehaviour
{
    [Inject]
    private HealthChangedEvent healthChangedEvent;

    [SerializeField]
    private float maxHealth = 100f;

    [SerializeField]
    private CharacterAnimator animator;

    public float MaxHealth => maxHealth;

    private CharacterStateStorage stateStorage;

    public void Initialize(CharacterStateStorage stateStorage)
    {
        this.stateStorage = stateStorage;
        stateStorage.OnHealthChanged += OnHealthChanged;
        RaiseHudEvent(stateStorage.Health);
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
            stateStorage.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float value)
    {
        RaiseHudEvent(value);
        UpdateAnimation(value);
    }

    private void RaiseHudEvent(float currentHealth)
    {
        if (!stateStorage.isLocalPlayer)
            return;

        healthChangedEvent?.Raise(new HealthChangedData(currentHealth, maxHealth));
    }

    private void UpdateAnimation(float currentHealth)
    {
        if (animator == null)
            return;

        if (currentHealth <= 0f)
        {
            animator.PlayDeath();
        }
        else if (currentHealth < maxHealth)
            animator.PlayDamaged();
        else
            animator.PlayIdle();
    }
}
