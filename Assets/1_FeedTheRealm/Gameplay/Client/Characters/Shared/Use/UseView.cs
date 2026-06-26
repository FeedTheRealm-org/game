using Cysharp.Threading.Tasks;
using FTR.Core.Client;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Client.EventChannels.Ticks;
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

    private float gunUpOffset = 0.15f;
    private float bowUpOffset = 0.5f;

    [Inject]
    private IObjectResolver resolver;

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
    private GameObject bowEffectInstance;
    private GameObject meleeEffectInstance;
    private GameObject healEffectInstance;

    private ParticleSystem gunParticleSystem;
    private ParticleSystem bowParticleSystem;
    private ParticleSystem meleeParticleSystem;
    private ParticleSystem healParticleSystem;

    private GameObject rangedTargetIndicator;
    private SpriteRenderer rangedTargetIndicatorRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Transform cameraPivot;
    private WeaponItemData weaponData;
    private ConsumableItemData consumableData;
    private string equippedItemId;
    private Vector3 direction;
    private int equipmentChangeSequence = 0;
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

    // Called for ALL characters (player + enemy) from ClientCharacterLinker
    public void SetUpWeaponVFX()
    {
        if (prefabProvider == null)
            prefabProvider = resolver.Resolve<ClientPrefabProvider>();

        FindCameraPivot();
        Transform spawnParent = cameraPivot != null ? cameraPivot : transform;

        if (prefabProvider.GunEffectPrefab != null)
        {
            gunEffectInstance = resolver.Instantiate(prefabProvider.GunEffectPrefab, spawnParent);
            gunParticleSystem = gunEffectInstance.GetComponent<ParticleSystem>();
            gunParticleSystem?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (prefabProvider.BowEffectPrefab != null)
        {
            bowEffectInstance = resolver.Instantiate(prefabProvider.BowEffectPrefab, spawnParent);
            bowParticleSystem = bowEffectInstance.GetComponent<ParticleSystem>();
            bowParticleSystem?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (prefabProvider.MeleeEffectPrefab != null)
        {
            meleeEffectInstance = resolver.Instantiate(
                prefabProvider.MeleeEffectPrefab,
                spawnParent
            );
            meleeParticleSystem = meleeEffectInstance.GetComponent<ParticleSystem>();
            meleeParticleSystem?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // Called only for local player from ClientPlayerLinker
    public void SetUpConsumableVFX()
    {
        if (prefabProvider == null)
            prefabProvider = resolver.Resolve<ClientPrefabProvider>();

        FindCameraPivot();
        Transform spawnParent = cameraPivot != null ? cameraPivot : transform;

        if (prefabProvider.HealEffectPrefab != null)
        {
            healEffectInstance = resolver.Instantiate(prefabProvider.HealEffectPrefab, spawnParent);
            healEffectInstance.transform.localScale = new Vector3(2, 2, 2);
            healEffectInstance.transform.localPosition = Vector3.zero;
            healParticleSystem = healEffectInstance.GetComponent<ParticleSystem>();
        }
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
            soundFxId = (weaponData.weaponType, weaponData.subWeaponType) switch
            {
                (WeaponType.Ranged, SubWeaponType.Bow) => ClientSoundFXRegistry
                    .SoundFXIds
                    .ArrowShot,
                (WeaponType.Ranged, _) => ClientSoundFXRegistry.SoundFXIds.HandgunShot,
                _ => ClientSoundFXRegistry.SoundFXIds.Attack,
            };
            PlayWeaponEffect();
        }
        else if (!string.IsNullOrEmpty(equippedItemId))
        {
            PlayHealEffect();
            soundFxId = ClientSoundFXRegistry.SoundFXIds.Consume;
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
            return;

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

    private void PlayWeaponEffect()
    {
        Debug.Log(
            $"[UseView] PlayWeaponEffect: weaponType={weaponData.weaponType} subType={weaponData.subWeaponType} | gun={gunEffectInstance != null} bow={bowEffectInstance != null} melee={meleeEffectInstance != null}"
        );

        var (effectInstance, particleSystem) = (
            weaponData.weaponType,
            weaponData.subWeaponType
        ) switch
        {
            (WeaponType.Ranged, SubWeaponType.Bow) => (bowEffectInstance, bowParticleSystem),
            (WeaponType.Ranged, _) => (gunEffectInstance, gunParticleSystem),
            (WeaponType.Melee, _) => (meleeEffectInstance, meleeParticleSystem),
            _ => (null, null),
        };

        if (effectInstance == null)
        {
            Debug.LogWarning(
                $"[UseView] PlayWeaponEffect: effectInstance is null for weaponType={weaponData.weaponType}"
            );
            return;
        }

        PositionEffectAtWeaponApex(effectInstance);

        if (direction != Vector3.zero && !IsMeleeWeapon(weaponData))
            effectInstance.transform.rotation = Quaternion.LookRotation(direction);

        if (IsBowType(weaponData))
            PlayDelayed(particleSystem, 0.2f).Forget();
        else
            particleSystem?.Play();
    }

    private async UniTaskVoid PlayDelayed(ParticleSystem ps, float delay)
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
        if (ps != null)
            ps.Play();
    }

    private void PositionEffectAtWeaponApex(GameObject effect)
    {
        var weaponApex = System.Array.Find(
            transform.root.GetComponentsInChildren<Transform>(),
            t => t.name == "AnchorWeaponApex"
        );

        if (weaponApex != null)
        {
            Vector3 pos = weaponApex.position;
            if (IsHandGunType(weaponData))
            {
                pos = FixGunPosition(pos);
                effect.transform.position = pos;
            }
            if (IsBowType(weaponData))
            {
                pos = FixBowPosition(pos);
                effect.transform.position = pos;
            }
            else if (IsMeleeWeapon(weaponData))
            {
                FixMeleePosition(effect, pos);
            }
        }
        else if (cameraPivot != null)
        {
            effect.transform.localPosition = Vector3.zero;
        }
    }

    private void PlayHealEffect()
    {
        if (
            healEffectInstance == null
            || consumableData == null
            || consumableData.effectType != EffectType.Heal
        )
            return;

        healEffectInstance.SetActive(true);
        healEffectInstance.transform.localPosition = Vector3.zero;
        healEffectInstance.transform.rotation = Quaternion.LookRotation(Vector3.up);
        if (healParticleSystem != null)
            healParticleSystem.Play();
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
        else if (IsRangedWeapon(weaponData))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction.normalized * weaponData.range);
        }
    }

    private Vector3 FixGunPosition(Vector3 pos)
    {
        pos.y += gunUpOffset;
        if (animator.CurrentFacing == FacingDirection.Back)
            pos.y += gunUpOffset * 2;
        else if (animator.CurrentFacing == FacingDirection.Front)
            pos.y -= gunUpOffset * 1.5f;
        else if (animator.CurrentFacing == FacingDirection.Right)
            pos += direction * (-gunUpOffset * 1.75f);
        else if (animator.CurrentFacing == FacingDirection.Left)
            pos += direction * (-gunUpOffset);
        return pos;
    }

    private Vector3 FixBowPosition(Vector3 pos)
    {
        pos.y += bowUpOffset;
        if (animator.CurrentFacing == FacingDirection.Front)
            pos.y -= bowUpOffset * 0.5f;
        else if (animator.CurrentFacing == FacingDirection.Back)
            pos.y += bowUpOffset * 2.5f;
        return pos;
    }

    private void FixMeleePosition(GameObject effect, Vector3 pos)
    {
        Quaternion meleeRotation =
            direction != Vector3.zero
                ? Quaternion.LookRotation(Vector3.up, direction) * Quaternion.Euler(0, 0, -90)
                : Quaternion.identity;
        effect.transform.SetPositionAndRotation(pos, meleeRotation);
    }

    private bool IsRangedWeapon(WeaponItemData weaponData)
    {
        return weaponData.weaponType == WeaponType.Ranged;
    }

    private bool IsBowType(WeaponItemData weaponData)
    {
        return IsRangedWeapon(weaponData) && weaponData.subWeaponType == SubWeaponType.Bow;
    }

    private bool IsMeleeWeapon(WeaponItemData weaponData)
    {
        return weaponData.weaponType == WeaponType.Melee;
    }

    private bool IsHandGunType(WeaponItemData weaponData)
    {
        return IsRangedWeapon(weaponData) && weaponData.subWeaponType == SubWeaponType.HandHeld;
    }
}
