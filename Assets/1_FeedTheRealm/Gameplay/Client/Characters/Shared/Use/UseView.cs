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
        rangedTargetIndicatorPrefab.SetActive(false);
        this.cameraPivot = transform
            .Find("CharacterBody")
            ?.transform.Find("CenterMarker")
            ?.transform;
        this.rangedTargetIndicator = resolver.Instantiate(rangedTargetIndicatorPrefab, cameraPivot);
        this.rangedTargetIndicatorRenderer =
            this.rangedTargetIndicator.GetComponent<SpriteRenderer>();
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
        ApplyEquippedItemAsync(newItemId).Forget();
    }

    private void OnAttackEvent(AttackEventContent attackEvent)
    {
        animator.SetAction(true);
        animator.PlayAttack();
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
    }

    private async UniTask ApplyEquippedItemAsync(string itemId)
    {
        rangedTargetIndicator?.SetActive(false);

        if (string.IsNullOrEmpty(itemId))
        {
            spriteManager.ChangeSprite(CharacterPartCategory.EquipmentR, null);
            return;
        }

        var itemData = ClientItemsRegistry.GetItemById(itemId);
        string spriteId =
            itemData != null && !string.IsNullOrEmpty(itemData.spriteFilePath)
                ? itemData.spriteFilePath
                : itemId;

        Debug.Log($"InventoryView applying equipped item: {itemId} with spriteId: {spriteId}");
        var texture = await itemsAssetsService.DownloadItemSpriteAsync(spriteId);

        if (this == null || spriteManager == null)
        {
            Debug.Log(
                $"InventoryView no longer valid after sprite download, aborting apply for {itemId}"
            );
            return;
        }

        var weaponData = ClientItemsRegistry.GetWeaponById(itemId);
        if (weaponData == null)
        {
            spriteManager.ChangeSprite(CharacterPartCategory.EquipmentR, texture);
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
    }

    private void UpdateIndicatorRange(float range)
    {
        if (rangedTargetIndicatorRenderer == null)
            return;

        // Normalize range (adjust maxVisualRange based on your game scale)
        float maxVisualRange = 20f;
        float normalizedRange = Mathf.Clamp01(range / maxVisualRange);

        // Use MaterialPropertyBlock to avoid creating material instances
        rangedTargetIndicatorRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_ArrowLength", normalizedRange);
        rangedTargetIndicatorRenderer.SetPropertyBlock(propertyBlock);
    }
}
