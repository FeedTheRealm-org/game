using FTR.Core.Client;
using FTR.Core.Common.Enums;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ConsumableEffectView : MonoBehaviour
{
    [Inject]
    private IObjectResolver resolver;

    [Inject]
    private ClientPrefabProvider prefabProvider;

    private GameObject speedUpEffectInstance;
    private GameObject damageEffectInstance;
    private CharacterStateStorage stateStorage;

    private GameObject speedScreenEffectInstance;
    private ParticleSystem speedScreenEffectParticles;
    private GameObject damageScreenEffectInstance;
    private ParticleSystem damageScreenEffectParticles;

    public void SetUp(CharacterStateStorage stateStorage)
    {
        this.stateStorage = stateStorage;
        stateStorage.ActiveEffectIds.Callback += OnActiveEffectListChanged;

        var spawnParent = FindSpawnParent();

        speedUpEffectInstance = resolver.Instantiate(
            prefabProvider.SpeedUpEffectPrefab,
            spawnParent
        );
        speedUpEffectInstance.transform.localScale = new Vector3(2, 2, 2);
        speedUpEffectInstance.transform.localPosition = Vector3.zero;
        speedUpEffectInstance.SetActive(false);

        damageEffectInstance = resolver.Instantiate(prefabProvider.DamageEffectPrefab, spawnParent);
        damageEffectInstance.transform.localScale = new Vector3(2, 2, 2);
        damageEffectInstance.transform.localPosition = Vector3.zero;
        damageEffectInstance.SetActive(false);

        foreach (var id in stateStorage.ActiveEffectIds)
            PlayEffectForId(id);
    }

    public void SetUpScreenEffects()
    {
        var renderCam = ScreenEffectSetup.FindRenderCamera();

        if (prefabProvider.SpeedUpScreenEffectPrefab != null)
            (speedScreenEffectInstance, speedScreenEffectParticles) = ScreenEffectSetup.Instantiate(
                resolver,
                prefabProvider.SpeedUpScreenEffectPrefab,
                renderCam
            );
        else
            Debug.LogWarning(
                "[ConsumableEffectView] SpeedUpScreenEffectPrefab is not assigned in ClientPrefabProvider."
            );

        if (prefabProvider.DamageScreenEffectPrefab != null)
            (damageScreenEffectInstance, damageScreenEffectParticles) =
                ScreenEffectSetup.Instantiate(
                    resolver,
                    prefabProvider.DamageScreenEffectPrefab,
                    renderCam
                );
        else
            Debug.LogWarning(
                "[ConsumableEffectView] DamageScreenEffectPrefab is not assigned in ClientPrefabProvider."
            );

        foreach (var id in stateStorage.ActiveEffectIds)
            PlayScreenEffectForId(id);
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
            stateStorage.ActiveEffectIds.Callback -= OnActiveEffectListChanged;
    }

    private void OnActiveEffectListChanged(
        SyncList<string>.Operation op,
        int index,
        string oldItem,
        string newItem
    )
    {
        switch (op)
        {
            case SyncList<string>.Operation.OP_ADD:
                PlayEffectForId(newItem);
                break;
            case SyncList<string>.Operation.OP_REMOVEAT:
                StopEffectForId(oldItem);
                break;
            case SyncList<string>.Operation.OP_CLEAR:
                StopAllEffects();
                break;
        }
    }

    private void PlayEffectForId(string itemId)
    {
        var consumable = ClientItemsRegistry.GetConsumableById(itemId);
        if (consumable == null)
            return;

        switch (consumable.effectType)
        {
            case EffectType.Speed:
                PlayEffect(speedUpEffectInstance);
                ScreenEffectSetup.Play(speedScreenEffectInstance, speedScreenEffectParticles);
                break;
            case EffectType.Damage:
                PlayEffect(damageEffectInstance);
                ScreenEffectSetup.Play(damageScreenEffectInstance, damageScreenEffectParticles);
                break;
        }
    }

    private void StopEffectForId(string itemId)
    {
        var consumable = ClientItemsRegistry.GetConsumableById(itemId);
        if (consumable == null)
            return;

        switch (consumable.effectType)
        {
            case EffectType.Speed:
                StopEffect(speedUpEffectInstance);
                ScreenEffectSetup.Stop(speedScreenEffectInstance, speedScreenEffectParticles);
                break;
            case EffectType.Damage:
                StopEffect(damageEffectInstance);
                ScreenEffectSetup.Stop(damageScreenEffectInstance, damageScreenEffectParticles);
                break;
        }
    }

    private void PlayScreenEffectForId(string itemId)
    {
        var consumable = ClientItemsRegistry.GetConsumableById(itemId);
        if (consumable == null)
            return;

        switch (consumable.effectType)
        {
            case EffectType.Speed:
                ScreenEffectSetup.Play(speedScreenEffectInstance, speedScreenEffectParticles);
                break;
            case EffectType.Damage:
                ScreenEffectSetup.Play(damageScreenEffectInstance, damageScreenEffectParticles);
                break;
        }
    }

    private void PlayEffect(GameObject effect)
    {
        if (effect == null)
            return;
        effect.transform.localRotation = Quaternion.identity;
        effect.SetActive(true);
        effect.transform.localPosition = Vector3.zero;
        effect.GetComponent<ParticleSystem>()?.Play();
    }

    private void StopAllEffects()
    {
        StopEffect(speedUpEffectInstance);
        StopEffect(damageEffectInstance);
        ScreenEffectSetup.Stop(speedScreenEffectInstance, speedScreenEffectParticles);
        ScreenEffectSetup.Stop(damageScreenEffectInstance, damageScreenEffectParticles);
    }

    private void StopEffect(GameObject effect)
    {
        if (effect == null)
            return;
        effect.GetComponent<ParticleSystem>()?.Stop();
        effect.SetActive(false);
    }

    private Transform FindSpawnParent()
    {
        var found = System.Array.Find(
            transform.GetComponentsInChildren<Transform>(true),
            t => t.name == "CenterMarker"
        );
        return found != null ? found : transform;
    }
}
