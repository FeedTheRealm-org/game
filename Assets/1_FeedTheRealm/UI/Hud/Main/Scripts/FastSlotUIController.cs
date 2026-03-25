using System.Collections.Generic;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

[RequireComponent(typeof(UIDocument))]
public class FastSlotUIController : MonoBehaviour
{
    private const int FastSlotCount = 5;

    [Inject]
    private LastAddedEvent lastAddedEvent;

    [Inject]
    private LastSwappedEvent lastSwappedEvent;

    [Inject]
    private LastRemovedEvent lastRemovedEvent;

    [Inject]
    private ActiveSlotChangedEvent activeSlotChangedEvent;

    [Inject]
    private SlotEquipRequestEvent equipRequestEvent;

    [Inject]
    private InventoryToggleEvent inventoryToggleEvent;

    [SerializeField]
    private PlayerInputReader inputReader;

    [SerializeField]
    private Sprite defaultSlotSprite;

    [SerializeField]
    private Sprite selectedSlotSprite;

    [SerializeField]
    private Sprite hiddenHUDSlotSprite;

    [SerializeField]
    private API.ItemAssetsService itemAssetsService;

    private UIDocument uiDocument;
    private readonly List<VisualElement> slots = new(FastSlotCount);
    private int activeSlot = 0;
    private bool isInventoryOpen = false;

    private void OnEnable()
    {
        uiDocument ??= GetComponent<UIDocument>();
        var root = uiDocument?.rootVisualElement;
        if (root == null)
            return;

        slots.Clear();
        for (int i = 0; i < FastSlotCount; i++)
        {
            var slot = root.Q($"FastEquipSlot{i + 1}");
            if (slot != null)
                slots.Add(slot);
        }

        lastAddedEvent.OnRaised += OnLastAdded;
        lastSwappedEvent.OnRaised += OnLastSwapped;
        lastRemovedEvent.OnRaised += OnLastRemoved;
        activeSlotChangedEvent.OnRaised += OnActiveSlotChanged;
        inventoryToggleEvent.OnRaised += OnInventoryToggled;
        inputReader.FastSlotEvent += OnFastSlotInput;

        SetActiveSlot(activeSlot);
    }

    private void OnDisable()
    {
        lastAddedEvent.OnRaised -= OnLastAdded;
        lastSwappedEvent.OnRaised -= OnLastSwapped;
        lastRemovedEvent.OnRaised -= OnLastRemoved;
        activeSlotChangedEvent.OnRaised -= OnActiveSlotChanged;
        inventoryToggleEvent.OnRaised -= OnInventoryToggled;
        inputReader.FastSlotEvent -= OnFastSlotInput;
    }

    // ────────────────────────── Input ─────────────────────────────────────────────

    private void OnFastSlotInput(int inputPad)
    {
        if (isInventoryOpen)
            return;
        if (inputPad <= 0 || inputPad > FastSlotCount)
            return;
        equipRequestEvent.Raise(inputPad - 1);
    }

    // ────────────────────────── Inventory events ─────────────────────────────────────
    private void OnLastAdded((StorageType storageType, string itemId, int position) data)
    {
        if (data.storageType != StorageType.FastSlot)
            return;
        SlotItemLoader.LoadItem(Icon(data.position), data.itemId, itemAssetsService);
    }

    private void OnLastRemoved((StorageType storageType, string itemId, int position) data)
    {
        if (data.storageType != StorageType.FastSlot)
            return;
        SlotItemLoader.LoadItem(Icon(data.position), null, itemAssetsService);
    }

    private void OnLastSwapped(
        (
            StorageType sourceType,
            int sourceSlot,
            string sourceItemId,
            StorageType targetType,
            int targetSlot,
            string targetItemId
        ) data
    )
    {
        if (data.targetType == StorageType.FastSlot)
            SlotItemLoader.LoadItem(Icon(data.targetSlot), data.sourceItemId, itemAssetsService);
        if (data.sourceType == StorageType.FastSlot)
            SlotItemLoader.LoadItem(Icon(data.sourceSlot), data.targetItemId, itemAssetsService);
    }

    // ────────────────────────── Active slot & visibility ──────────────────────────────────

    private void OnActiveSlotChanged(int slotIndex) => SetActiveSlot(slotIndex);

    private void OnInventoryToggled(bool status)
    {
        isInventoryOpen = status;
        for (int i = 0; i < slots.Count; i++)
        {
            Sprite sprite =
                status ? hiddenHUDSlotSprite
                : i == activeSlot ? selectedSlotSprite
                : defaultSlotSprite;
            SetSlotBackground(i, sprite);
        }
    }

    private void SetActiveSlot(int slotIndex)
    {
        SetSlotBackground(activeSlot, defaultSlotSprite);
        activeSlot = slotIndex;
        SetSlotBackground(activeSlot, selectedSlotSprite);
    }

    // ────────────────────────── Helpers ──────────────────────────────────────

    private void SetSlotBackground(int index, Sprite sprite)
    {
        if (index < 0 || index >= slots.Count || sprite == null)
            return;
        slots[index].style.backgroundImage = new StyleBackground(sprite);
    }

    private VisualElement Icon(int index)
    {
        if (index < 0 || index >= slots.Count)
            return null;
        return slots[index].Q("FastEquipIcon") ?? slots[index];
    }
}
