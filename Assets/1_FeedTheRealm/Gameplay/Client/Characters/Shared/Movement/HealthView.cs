using System;
using System.Collections;
using FTR.Core.Client.EventChannels.Status;
using FTR.Core.Common.Config;
using FTR.Core.Common.Systems.Status;
using FTR.Gameplay.Client.Registry;
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
    private Config config;

    [Inject]
    private Logging.Logger logger;

    [SerializeField]
    private CharacterAnimator animator;

    [Inject]
    private ISoundPlayer soundPlayer;

    public event Action<float> OnMaxHealthInitialized;
    public bool IsMaxHealthInitialized => isMaxHealthInitialized;

    private float maxHealth;
    public bool isMaxHealthInitialized = false;

    public float MaxHealth
    {
        get => maxHealth > 0 ? maxHealth : config.playerMaxHealth;
        set
        {
            bool wasInitialized = isMaxHealthInitialized;
            maxHealth = value;
            isMaxHealthInitialized = true;

            if (!wasInitialized)
                OnMaxHealthInitialized?.Invoke(maxHealth);

            if (wasInitialized && stateStorage != null)
                RaiseHudEvent(stateStorage.Health);
        }
    }

    private CharacterStateStorage stateStorage;

    public void Initialize(CharacterStateStorage stateStorage)
    {
        this.stateStorage = stateStorage;

        if (
            stateStorage.Health >= config.playerMaxHealth
            && !string.IsNullOrEmpty(stateStorage.CharacterId)
        )
        {
            stateStorage.OnHealthChanged += DeferredInit;
        }
        else
        {
            if (stateStorage != null && string.IsNullOrEmpty(stateStorage.CharacterId))
                stateStorage.OnCharacterIdChanged += InitForCharacterId;
            else
                InitForCharacterId(stateStorage.CharacterId);

            stateStorage.OnHealthChanged += OnHealthChanged;
        }
    }

    private void DeferredInit(float health)
    {
        stateStorage.OnHealthChanged -= DeferredInit;

        if (stateStorage != null && string.IsNullOrEmpty(stateStorage.CharacterId))
            stateStorage.OnCharacterIdChanged += InitForCharacterId;
        else
            InitForCharacterId(stateStorage.CharacterId);

        stateStorage.OnHealthChanged += OnHealthChanged;

        OnHealthChanged(health);
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
        {
            stateStorage.OnHealthChanged -= OnHealthChanged;
            stateStorage.OnCharacterIdChanged -= InitForCharacterId;
        }
    }

    private void OnHealthChanged(float value)
    {
        StartCoroutine(UpdateHealthAfterDelay(value));
    }

    private IEnumerator UpdateHealthAfterDelay(float value)
    {
        if (value < MaxHealth)
            yield return new WaitForSeconds(config.HealthUpdateDelay);

        RaiseHudEvent(value);
        UpdateAnimation(value);
    }

    private void RaiseHudEvent(float currentHealth)
    {
        if (!stateStorage.isLocalPlayer)
            return;

        healthChangedEvent?.Raise(new HealthChangedData(currentHealth, MaxHealth));
    }

    private void UpdateAnimation(float currentHealth)
    {
        if (animator == null)
            return;

        if (currentHealth <= 0f)
        {
            animator.PlayDeath();
            soundPlayer.Play(ClientSoundFXRegistry.SoundFXIds.Death, transform.position);
        }
        else if (currentHealth < MaxHealth)
        {
            animator.PlayDamaged();
            soundPlayer.Play(ClientSoundFXRegistry.SoundFXIds.Hit, transform.position);
        }
        else
            animator.PlayIdle();
    }

    private void InitForCharacterId(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return;

        var enemyData = ClientItemsRegistry.GetEnemyById(characterId);
        if (enemyData != null)
            MaxHealth = enemyData.healthPoints;
        else
            MaxHealth = config.playerMaxHealth;

        RaiseHudEvent(stateStorage.Health);
    }
}
