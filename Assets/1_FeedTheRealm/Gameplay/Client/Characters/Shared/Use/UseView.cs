using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Cysharp.Threading.Tasks;
using FTR.Core.Client;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTRShared.Runtime.Core.Cache;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class UseView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    [Header("Gun VFX Alignment")]
    [SerializeField]
    private float gunForwardOffset = 0.7f;

    [SerializeField]
    private float gunUpOffset = 1.5f;

    [Inject]
    IObjectResolver resolver;

    [Inject]
    private LateTickEvent lateTickEvent;

    [Inject]
    private CacheManager cacheManager;

    [Inject]
    private WorldSelector worldSelector;

    [Inject]
    private ISoundPlayer soundPlayer;

    private ClientPrefabProvider prefabProvider;
    private NetworkEventRouter eventRouter;
    private CharacterStateStorage stateStorage;
    private SpriteManager spriteManager;
    private GameObject gunEffectInstance;
    private GameObject healEffectInstance;
    private GameObject speedUpEffectInstance;
    private GameObject damageEffectInstance;
    private GameObject rangedTargetIndicator;
    private SpriteRenderer rangedTargetIndicatorRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Transform cameraPivot;
    private WeaponItemData weaponData;
    private ConsumableItemData consumableData;
    private string equippedItemId;
    private Vector3 direction;
    private int equipmentChangeSequence = 0;
    private Character characterComponent;
    private Transform fireMuzzleTransform;

    private bool isInitialized = false;

    public void Initialize(
        NetworkEventRouter eventRouter,
        CharacterStateStorage stateStorage,
        SpriteManager spriteManager
    )
    {
        this.eventRouter = eventRouter;
        this.stateStorage = stateStorage;
        this.spriteManager = spriteManager;
        eventRouter.OnAttackEvent += OnAttackEvent;
        stateStorage.OnEquippedItemChanged += OnEquippedItemChanged;
        lateTickEvent.OnRaised += LateTick;

        FindCameraPivot();

        this.propertyBlock = new MaterialPropertyBlock();

        isInitialized = true;
    }

    private void FindCameraPivot()
    {
        if (cameraPivot == null)
        {
            cameraPivot = transform
                .Find("CharacterBody")
                ?.transform.Find("CenterMarker")
                ?.transform;
        }
    }

    public void SetRangedTargetIndicator(GameObject rangedTargetIndicatorPrefab)
    {
        FindCameraPivot();
        this.rangedTargetIndicator = resolver.Instantiate(rangedTargetIndicatorPrefab, cameraPivot);
        this.rangedTargetIndicator.SetActive(false);
        this.rangedTargetIndicatorRenderer =
            this.rangedTargetIndicator.GetComponentInChildren<SpriteRenderer>();
    }

    private void OnDestroy()
    {
        if (eventRouter != null)
            eventRouter.OnAttackEvent -= OnAttackEvent;
        if (stateStorage != null)
            stateStorage.OnEquippedItemChanged -= OnEquippedItemChanged;
        if (lateTickEvent != null)
            lateTickEvent.OnRaised -= LateTick;
    }

    private void OnEquippedItemChanged(string newItemId)
    {
        if (!isInitialized)
            return;
        var seq = ++equipmentChangeSequence;
        ApplyEquippedItemAsync(newItemId, seq).Forget();
    }

    private void OnAttackEvent(AttackEventContent attackEvent)
    {
        animator.SetAction(true);
        animator.PlayUse();

        string soundFxId = ClientSoundFXRegistry.SoundFXIds.Attack;

        if (weaponData != null)
        {
            switch (weaponData.weaponType)
            {
                case WeaponType.Melee:
                    soundFxId = ClientSoundFXRegistry.SoundFXIds.Attack;
                    break;
                case WeaponType.Ranged:
                    if (weaponData.subWeaponType == SubWeaponType.Bow)
                        soundFxId = ClientSoundFXRegistry.SoundFXIds.ArrowShot;
                    else
                    {
                        soundFxId = ClientSoundFXRegistry.SoundFXIds.HandgunShot;
                        PlayGunEffect();
                    }
                    break;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(equippedItemId))
            {
                PlayConsumableEffect(consumableData);
                soundFxId = ClientSoundFXRegistry.SoundFXIds.Consume;
            }
        }

        soundPlayer.Play(soundFxId, transform.position);
    }

    private void LateTick()
    {
        if (cameraPivot == null)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        Vector3 dir = animator.CurrentFacing switch
        {
            FacingDirection.Left => -cam.transform.right,
            FacingDirection.Right => cam.transform.right,
            FacingDirection.Back => cam.transform.forward,
            FacingDirection.Front => -cam.transform.forward,
            _ => cam.transform.forward,
        };
        dir.y = 0f;
        dir.Normalize();

        direction = dir;

        if (rangedTargetIndicator != null && rangedTargetIndicator.activeSelf)
        {
            Vector3 pivot = cameraPivot.position;
            pivot.y = rangedTargetIndicator.transform.position.y;

            rangedTargetIndicator.transform.position = pivot;
            rangedTargetIndicator.transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private async UniTask ApplyEquippedItemAsync(string itemId, int sequenceAtCall)
    {
        rangedTargetIndicator?.SetActive(false);

        if (string.IsNullOrEmpty(itemId))
        {
            this.equippedItemId = null;
            this.weaponData = null;
            spriteManager.ChangeSprite(CharacterPartCategory.EquipmentR, null);
            animator.UnSetWeaponType();
            return;
        }

        var itemData = ClientItemsRegistry.GetItemById(itemId);
        string spriteReference =
            itemData != null && !string.IsNullOrEmpty(itemData.spriteFilePath)
                ? itemData.spriteFilePath
                : itemId;

        string worldId = worldSelector?.GetSelectedWorldId();
        string fileName = System.IO.Path.GetFileName(spriteReference);
        spriteReference = $"/worlds/{worldId}/items/{fileName}";

        var texture = await cacheManager.GetSprite(
            spriteReference,
            worldSelector.GetSelectedWorldUpdatedAt()
        );
        if (sequenceAtCall != equipmentChangeSequence)
            return; // Used to prevent race conditions when fast switching items

        this.equippedItemId = itemId;

        if (this == null || spriteManager == null)
        {
            return;
        }

        var weaponData = ClientItemsRegistry.GetWeaponById(itemId);
        this.weaponData = weaponData;
        if (weaponData == null)
        {
            this.consumableData = ClientItemsRegistry.GetConsumableById(itemId);
            spriteManager.ChangeSprite(CharacterPartCategory.Consumable, texture);
            animator.SetEquipment(default, default);
            return;
        }

        switch (weaponData.weaponType)
        {
            case WeaponType.Melee:
                spriteManager.ChangeSprite(CharacterPartCategory.EquipmentR, texture);
                break;
            case WeaponType.Ranged:
                if (weaponData.subWeaponType == SubWeaponType.Bow)
                    spriteManager.ChangeSprite(CharacterPartCategory.WeaponRangedBow, texture);
                else
                    spriteManager.ChangeSprite(CharacterPartCategory.WeaponRangedHandheld, texture);
                UpdateIndicatorRange(weaponData.range);
                rangedTargetIndicator?.SetActive(true);
                break;
            default:
                spriteManager.ChangeSprite(CharacterPartCategory.EquipmentR, null);
                break;
        }
        animator.SetEquipment(weaponData.weaponType, weaponData.subWeaponType);
    }

    private void UpdateIndicatorRange(float range)
    {
        if (rangedTargetIndicatorRenderer == null)
        {
            Debug.LogWarning(
                "Ranged target indicator renderer is not set. Cannot update indicator range."
            );
            return;
        }

        float meshWidthX = rangedTargetIndicatorRenderer.bounds.size.x;
        float meshWidthZ = rangedTargetIndicatorRenderer.bounds.size.z;
        float meshWorldWidth = Mathf.Max(meshWidthX, meshWidthZ);

        float shaderArrowLength = (range / meshWorldWidth) * 10;
        rangedTargetIndicatorRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_ArrowLength", shaderArrowLength);
        rangedTargetIndicatorRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetUpUseViewVFX()
    {
        if (prefabProvider == null)
            prefabProvider = resolver.Resolve<ClientPrefabProvider>();

        characterComponent = transform.root.GetComponentInChildren<Character>(true);

        if (characterComponent != null)
        {
            fireMuzzleTransform = characterComponent.AnchorFireMuzzle;
        }

        FindCameraPivot();
        Transform spawnParent = cameraPivot != null ? cameraPivot : transform;

        if (prefabProvider.GunEffectPrefab != null)
        {
            this.gunEffectInstance = resolver.Instantiate(
                prefabProvider.GunEffectPrefab,
                spawnParent
            );
        }

        this.healEffectInstance = resolver.Instantiate(
            prefabProvider.HealEffectPrefab,
            spawnParent
        );
        this.healEffectInstance.transform.localScale = new Vector3(2, 2, 2);
        this.healEffectInstance.transform.localPosition = Vector3.zero;

        this.speedUpEffectInstance = resolver.Instantiate(
            prefabProvider.SpeedUpEffectPrefab,
            spawnParent
        );
        this.speedUpEffectInstance.transform.localScale = new Vector3(2, 2, 2);
        this.speedUpEffectInstance.transform.localPosition = Vector3.zero;
        this.speedUpEffectInstance.SetActive(false);

        this.damageEffectInstance = resolver.Instantiate(
            prefabProvider.GunEffectPrefab,
            spawnParent
        );
        this.damageEffectInstance.transform.localScale = new Vector3(2, 2, 2);
        this.damageEffectInstance.transform.localPosition = Vector3.zero;
        this.damageEffectInstance.SetActive(false);
    }

    private void PlayGunEffect()
    {
        if (gunEffectInstance == null)
            return;

        if (fireMuzzleTransform != null)
        {
            Vector3 dynamicOffset = (direction * gunForwardOffset) + (Vector3.up * gunUpOffset);
            this.gunEffectInstance.transform.position =
                fireMuzzleTransform.position + dynamicOffset;
        }
        else if (cameraPivot != null)
        {
            this.gunEffectInstance.transform.localPosition = Vector3.zero;
        }

        if (direction != Vector3.zero)
            this.gunEffectInstance.transform.rotation = Quaternion.LookRotation(direction);

        var ps = this.gunEffectInstance.GetComponent<ParticleSystem>();
        ps?.Play();
    }

    private void PlayConsumableEffect(ConsumableItemData consumableData)
    {
        if (consumableData == null)
            return;

        GameObject activeEffect = null;
        bool shouldFaceUp = false;

        if (consumableData.effectType == EffectType.Heal)
        {
            activeEffect = this.healEffectInstance;
            shouldFaceUp = true;
        }
        else if (consumableData.effectType == EffectType.Speed)
        {
            activeEffect = this.speedUpEffectInstance;
            shouldFaceUp = true;
        }
        else if (consumableData.effectType == EffectType.Damage)
        {
            activeEffect = this.damageEffectInstance;
        }

        if (activeEffect != null)
        {
            activeEffect.SetActive(true);
            activeEffect.transform.localPosition = Vector3.zero;

            if (shouldFaceUp)
            {
                activeEffect.transform.rotation = Quaternion.LookRotation(Vector3.up);
            }
            else
            {
                if (direction != Vector3.zero)
                {
                    activeEffect.transform.rotation = Quaternion.LookRotation(direction);
                }
            }

            var ps = activeEffect.GetComponent<ParticleSystem>();
            ps?.Play();
        }
    }

    private void OnDrawGizmos()
    {
        if (this.weaponData == null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
            return;
        }

        if (weaponData.weaponType == WeaponType.Melee)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, weaponData.range);
        }
        else if (weaponData.weaponType == WeaponType.Ranged)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction.normalized * weaponData.range);
        }
    }
}
