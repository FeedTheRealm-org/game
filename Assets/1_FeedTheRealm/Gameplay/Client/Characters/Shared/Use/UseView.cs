using Cysharp.Threading.Tasks;
using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class UseView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    [Inject]
    IObjectResolver resolver;

    [Inject]
    private LateTickEvent lateTickEvent;

    [Inject]
    private API.ItemAssetsService itemsAssetsService;

    private NetworkEventRouter eventRouter;
    private CharacterStateStorage stateStorage;
    private SpriteManager spriteManager;

    private GameObject rangedTargetIndicator;
    private SpriteRenderer rangedTargetIndicatorRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Transform cameraPivot;
    private WeaponItemData weaponData;
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

        this.propertyBlock = new MaterialPropertyBlock();

        isInitialized = true;
    }

    public void SetRangedTargetIndicator(GameObject rangedTargetIndicatorPrefab)
    {
        this.cameraPivot = transform
            .Find("CharacterBody")
            ?.transform.Find("CenterMarker")
            ?.transform;
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
        ApplyEquippedItemAsync(newItemId, equipmentChangeSequence).Forget();
    }

    private void OnAttackEvent(AttackEventContent attackEvent)
    {
        animator.SetAction(true);
        animator.PlayUse();
    }

    private void LateTick()
    {
        if (
            rangedTargetIndicator == null
            || !rangedTargetIndicator.activeSelf
            || cameraPivot == null
        )
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

        Vector3 pivot = cameraPivot.position;
        pivot.y = rangedTargetIndicator.transform.position.y;

        rangedTargetIndicator.transform.position = pivot;
        rangedTargetIndicator.transform.rotation = Quaternion.LookRotation(dir);

        direction = dir;
    }

    private async UniTask ApplyEquippedItemAsync(string itemId, int sequenceAtCall)
    {
        rangedTargetIndicator?.SetActive(false);

        if (string.IsNullOrEmpty(itemId))
        {
            spriteManager.ChangeSprite(CharacterPartCategory.EquipmentR, null);
            animator.UnSetWeaponType();
            return;
        }

        var itemData = ClientItemsRegistry.GetItemById(itemId);
        string spriteId =
            itemData != null && !string.IsNullOrEmpty(itemData.spriteFilePath)
                ? itemData.spriteFilePath
                : itemId;

        var texture = await itemsAssetsService.DownloadItemSpriteAsync(spriteId);
        if (sequenceAtCall != equipmentChangeSequence)
            return; // Used to prevent race conditions when fast switching items

        if (this == null || spriteManager == null)
        {
            return;
        }

        var weaponData = ClientItemsRegistry.GetWeaponById(itemId);
        this.weaponData = weaponData;
        if (weaponData == null)
        {
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
        equipmentChangeSequence++;
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
        Debug.Log(
            $"Updated ranged indicator for range {range} with meshWorldWidth {meshWorldWidth} and shaderArrowLength {shaderArrowLength}"
        );
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
