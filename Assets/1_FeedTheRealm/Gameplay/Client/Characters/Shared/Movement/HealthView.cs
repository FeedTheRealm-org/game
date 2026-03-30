using System.Collections;
using FTR.Core.Client.Config;
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

    [Inject]
    private ClientConfig config;

    [SerializeField]
    private CharacterAnimator animator;

    public float MaxHealth => config.MaxHealth;

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
        StartCoroutine(UpdateHealthAfterDelay(value));
    }

    private IEnumerator UpdateHealthAfterDelay(float value)
    {
        if (value < config.MaxHealth)
            yield return new WaitForSeconds(config.HealthUpdateDelay); // Delay for better animation timing

        RaiseHudEvent(value);
        UpdateAnimation(value);
    }

    private void RaiseHudEvent(float currentHealth)
    {
        if (!stateStorage.isLocalPlayer)
            return;

        healthChangedEvent?.Raise(new HealthChangedData(currentHealth, config.MaxHealth));
    }

    private void UpdateAnimation(float currentHealth)
    {
        if (animator == null)
            return;

        if (currentHealth <= 0f)
        {
            animator.PlayDeath();
        }
        else if (currentHealth < config.MaxHealth)
            animator.PlayDamaged();
        else
            animator.PlayIdle();
    }
}
