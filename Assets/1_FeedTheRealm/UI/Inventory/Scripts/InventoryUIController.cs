using System;
using System.Collections.Generic;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

[RequireComponent(typeof(UIDocument))]
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
    private Sprite itemObtainedSprite;

    private UIDocument uiDocument;
    private readonly List<VisualElement> inventorySlots = new(InventorySlotCount);
    private readonly List<VisualElement> fastSlots = new(FastSlotCount);

    private int selectedSlotIndex = -1;
    private StorageType selectedStorage;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;
        if (root == null)
            return;

        RegisterSlots(root, inventorySlots, InventorySlotCount, "Slot", OnInventorySlotClicked);
        RegisterSlots(root, fastSlots, FastSlotCount, "FastEquipSlot", OnFastSlotClicked);
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
        Action<int> onClick
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
            list.Add(slot);
        }
    }

    private void OnInventoryInput()
    {
        var inventory = uiDocument.rootVisualElement.Q("Inventory");
        if (inventory == null)
            return;
        bool show = inventory.resolvedStyle.display == DisplayStyle.None;
        inventory.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        inventoryToggleEvent?.Raise(show);
    }

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
    }

    private void SetSlotSprite(List<VisualElement> slots, int index, Sprite sprite)
    {
        if (sprite != null && index >= 0 && index < slots.Count)
            slots[index].style.backgroundImage = new StyleBackground(sprite);
    }

    private void OnLastAdded((StorageType t, string id, int pos) data) =>
        SetSlotItem(data.t, data.pos, data.id);

    private void OnLastRemoved((StorageType t, string id, int pos) data) =>
        SetSlotItem(data.t, data.pos, null);

    private void OnLastSwapped(
        (StorageType srcT, int srcI, string srcId, StorageType tgtT, int tgtI, string tgtId) data
    )
    {
        var srcIcon = Icon(data.srcT, data.srcI);
        var tgtIcon = Icon(data.tgtT, data.tgtI);
        if (srcIcon == null || tgtIcon == null)
            return;

        (srcIcon.style.backgroundImage, tgtIcon.style.backgroundImage) = (
            tgtIcon.style.backgroundImage,
            srcIcon.style.backgroundImage
        );
        (srcIcon.style.unityBackgroundImageTintColor, tgtIcon.style.unityBackgroundImageTintColor) =
            (
                tgtIcon.style.unityBackgroundImageTintColor,
                srcIcon.style.unityBackgroundImageTintColor
            );
    }

    private void SetSlotItem(StorageType storage, int index, string itemId)
    {
        var icon = Icon(storage, index);
        if (icon == null)
            return;

        bool empty = string.IsNullOrEmpty(itemId);
        icon.style.backgroundImage = empty ? null : new StyleBackground(itemObtainedSprite);
        icon.style.unityBackgroundImageTintColor = empty ? Color.white : ItemColor(itemId);
    }

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

    private Color ItemColor(string itemId)
    {
        var r = new System.Random(itemId.GetHashCode());
        return new Color(
            (float)r.NextDouble() * 0.7f + 0.3f,
            (float)r.NextDouble() * 0.7f + 0.3f,
            (float)r.NextDouble() * 0.7f + 0.3f
        );
    }
}
