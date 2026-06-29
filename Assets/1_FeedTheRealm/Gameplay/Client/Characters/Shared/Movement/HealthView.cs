using System;
using System.Collections;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.Status;
using FTR.Core.Common.Config;
using FTR.Core.Common.Systems.Status;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;
using VContainer.Unity;

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

    [Inject]
    private IObjectResolver resolver;

    [Inject]
    private ClientPrefabProvider prefabProvider;

    public event Action<float> OnMaxHealthInitialized;
    public bool IsMaxHealthInitialized => isMaxHealthInitialized;

    private float maxHealth;
    public bool isMaxHealthInitialized = false;

    private float previousHealth = -1f;
    private GameObject hitEffectInstance;
    private ParticleSystem hitParticleSystem;
    private Renderer hitParticleRenderer;
    private const float HitCameraOffset = 0.18f;
    private const float HitHeightOffset = 0.5f;
    private const int HitDepthSortingPrecision = 100;

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

        if (prefabProvider != null && prefabProvider.HitEffectPrefab != null)
        {
            var spawnParent = FindCenterMarker();
            hitEffectInstance = resolver.Instantiate(prefabProvider.HitEffectPrefab, spawnParent);
            hitEffectInstance.transform.localPosition = Vector3.zero;
            hitEffectInstance.SetActive(false);
            hitParticleSystem = hitEffectInstance.GetComponent<ParticleSystem>();
            hitParticleRenderer = hitEffectInstance.GetComponent<Renderer>();

            var sortingGroup = transform.GetComponentInChildren<SortingGroup>(true);
            if (hitParticleRenderer != null && sortingGroup != null)
                hitParticleRenderer.sortingLayerID = sortingGroup.sortingLayerID;
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
        bool tookDamage = previousHealth >= 0 && value < previousHealth;
        previousHealth = value;
        StartCoroutine(UpdateHealthAfterDelay(value, tookDamage));
    }

    private IEnumerator UpdateHealthAfterDelay(float value, bool tookDamage)
    {
        if (value < MaxHealth)
            yield return new WaitForSeconds(config.HealthUpdateDelay);

        if (tookDamage)
            PlayHitEffect();

        RaiseHudEvent(value);
        UpdateAnimation(value);
    }

    private void PlayHitEffect()
    {
        if (hitEffectInstance == null || hitParticleSystem == null)
        {
            Debug.LogWarning(
                $"[HealthView] PlayHitEffect skipped: instance={hitEffectInstance != null} ps={hitParticleSystem != null}"
            );
            return;
        }

        hitEffectInstance.transform.localPosition = Vector3.zero;
        Vector3 parentWorld = hitEffectInstance.transform.position;
        Vector3 worldPos = parentWorld;
        worldPos.y += HitHeightOffset;

        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 toCam = cam.transform.position - worldPos;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0f)
                worldPos += toCam.normalized * HitCameraOffset;
        }

        hitEffectInstance.transform.position = worldPos;
        Debug.Log(
            $"[HealthView] PlayHitEffect: parentWorld={parentWorld} finalWorld={worldPos} localPos={hitEffectInstance.transform.localPosition}"
        );

        hitEffectInstance.SetActive(true);
        hitParticleSystem.Play();
    }

    private void LateUpdate()
    {
        if (
            hitParticleRenderer == null
            || hitEffectInstance == null
            || !hitEffectInstance.activeSelf
        )
            return;
        var cam = Camera.main;
        if (cam == null)
            return;
        float depth = Vector3.Dot(hitEffectInstance.transform.position, cam.transform.forward);
        hitParticleRenderer.sortingOrder = Mathf.RoundToInt(-depth * HitDepthSortingPrecision);
    }

    private Transform FindCenterMarker()
    {
        var found = Array.Find(
            transform.GetComponentsInChildren<Transform>(true),
            t => t.name == "CenterMarker"
        );
        return found != null ? found : transform;
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
            soundPlayer.Play(ClientSoundFXRegistry.SoundFXIds.Hit, transform.position);
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
