using System;
using System.Collections.Generic;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(AnimationInventoryUIController))]
public class InventoryUIController : MonoBehaviour
{
    private const int InventorySlotCount = 12;
    private const int FastSlotCount = 5;

    [Inject]
    private LastAddedEvent lastAddedEvent;

    [Inject]
    private LastSwappedEvent lastSwappedEvent;

    [Inject]
    private LastRemovedEvent lastRemovedEvent;

    [Inject]
    private SlotSwapRequestEvent swapRequestEvent;

    [Inject]
    private SlotDropRequestEvent dropRequestEvent;

    [Inject]
    private InventoryToggleEvent inventoryToggleEvent;

    [SerializeField]
    private PlayerInputReader inputReader;

    [SerializeField]
    private Sprite defaultSlotSprite;

    [SerializeField]
    private Sprite selectedSlotSprite;

    [SerializeField]
    private API.ItemAssetsService itemAssetsService;

    private UIDocument uiDocument;
    private AnimationInventoryUIController animationController;

    private readonly List<VisualElement> inventorySlots = new(InventorySlotCount);
    private readonly List<VisualElement> fastSlots = new(FastSlotCount);

    private int selectedSlotIndex = -1;
    private StorageType selectedStorage;

    private readonly InventorySlotGhostController ghost = new();

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        animationController = GetComponent<AnimationInventoryUIController>();

        var root = uiDocument.rootVisualElement;
        if (root == null)
            return;

        animationController.Initialize(root);

        RegisterSlots(
            root,
            inventorySlots,
            InventorySlotCount,
            "Slot",
            OnInventorySlotClicked,
            StorageType.Inventory
        );
        RegisterSlots(
            root,
            fastSlots,
            FastSlotCount,
            "FastEquipSlot",
            OnFastSlotClicked,
            StorageType.FastSlot
        );
        root.Q("Drop")?.RegisterCallback<ClickEvent>(_ => OnDropClicked());

        inputReader.InventoryEvent += OnInventoryInput;
        lastAddedEvent.OnRaised += OnLastAdded;
        lastSwappedEvent.OnRaised += OnLastSwapped;
        lastRemovedEvent.OnRaised += OnLastRemoved;
    }

    private void OnDisable()
    {
        inputReader.InventoryEvent -= OnInventoryInput;
        lastAddedEvent.OnRaised -= OnLastAdded;
        lastSwappedEvent.OnRaised -= OnLastSwapped;
        lastRemovedEvent.OnRaised -= OnLastRemoved;
    }

    private void RegisterSlots(
        VisualElement root,
        List<VisualElement> list,
        int count,
        string prefix,
        Action<int> onClick,
        StorageType storage
    )
    {
        list.Clear();
        for (int i = 0; i < count; i++)
        {
            var slot = root.Q($"{prefix}{i + 1}");
            if (slot == null)
                continue;

            int idx = i;
            slot.RegisterCallback<ClickEvent>(_ => onClick(idx));
            slot.RegisterCallback<PointerEnterEvent>(_ =>
                ghost.OnHoverEnter(selectedStorage, selectedSlotIndex, storage, idx, Icon)
            );
            slot.RegisterCallback<PointerLeaveEvent>(_ => ghost.OnHoverLeave());
            list.Add(slot);
        }
    }

    // ──────────────────────────────────── Input ─────────────────────────────────────────

    private void OnInventoryInput()
    {
        bool show = !animationController.IsVisible;
        animationController.Toggle();
        inventoryToggleEvent?.Raise(show);
    }

    // ───────────────────────────────── Slot click ───────────────────────────────────────

    private void OnInventorySlotClicked(int i) => HandleSlotClick(StorageType.Inventory, i);

    private void OnFastSlotClicked(int i) => HandleSlotClick(StorageType.FastSlot, i);

    private void HandleSlotClick(StorageType storage, int index)
    {
        if (selectedStorage == StorageType.Null)
        {
            SetSelection(storage, index);
            return;
        }

        if (selectedStorage == storage && selectedSlotIndex == index)
        {
            ClearSelection();
            return;
        }

        swapRequestEvent?.Raise((selectedStorage, selectedSlotIndex, storage, index));
        ClearSelection();
    }

    private void OnDropClicked()
    {
        if (selectedStorage == StorageType.Null)
            return;
        dropRequestEvent?.Raise((selectedStorage, selectedSlotIndex));
        ClearSelection();
    }

    private void SetSelection(StorageType storage, int index)
    {
        selectedStorage = storage;
        selectedSlotIndex = index;
        SetSlotSprite(Slots(storage), index, selectedSlotSprite);
    }

    private void ClearSelection()
    {
        if (selectedStorage != StorageType.Null)
            SetSlotSprite(Slots(selectedStorage), selectedSlotIndex, defaultSlotSprite);

        selectedStorage = StorageType.Null;
        selectedSlotIndex = -1;
        ghost.OnHoverLeave();
    }

    private void SetSlotSprite(List<VisualElement> slots, int index, Sprite sprite)
    {
        if (sprite != null && index >= 0 && index < slots.Count)
            slots[index].style.backgroundImage = new StyleBackground(sprite);
    }

    // ─────────────────────── Inventory events ──────────────────────────────────

    private void OnLastAdded((StorageType t, string id, int pos) data) =>
        SlotItemLoader.LoadItem(Icon(data.t, data.pos), data.id, itemAssetsService);

    private void OnLastRemoved((StorageType t, string id, int pos) data) =>
        SlotItemLoader.LoadItem(Icon(data.t, data.pos), null, itemAssetsService);

    private void OnLastSwapped(
        (StorageType srcT, int srcI, string srcId, StorageType tgtT, int tgtI, string tgtId) data
    )
    {
        SlotItemLoader.LoadItem(Icon(data.srcT, data.srcI), data.tgtId, itemAssetsService);
        SlotItemLoader.LoadItem(Icon(data.tgtT, data.tgtI), data.srcId, itemAssetsService);
    }

    // ────────────────────────────── Helpers ──────────────────────────────────────────────────────

    private List<VisualElement> Slots(StorageType t) =>
        t == StorageType.FastSlot ? fastSlots : inventorySlots;

    private VisualElement Icon(StorageType t, int index)
    {
        var slots = Slots(t);
        if (index < 0 || index >= slots.Count)
            return null;
        string iconName = t == StorageType.FastSlot ? "FastEquipIcon" : "ItemIcon";
        return slots[index].Q(iconName) ?? slots[index];
    }
}
