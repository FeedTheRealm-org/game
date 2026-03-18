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
    private Sprite itemObtainedSprite;

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
            if (slot == null)
                continue;
            slots.Add(slot);
        }

        lastSwappedEvent.OnRaised += OnLastSwapped;
        lastRemovedEvent.OnRaised += OnLastRemoved;
        activeSlotChangedEvent.OnRaised += OnActiveSlotChanged;
        inventoryToggleEvent.OnRaised += OnInventoryToggled;
        inputReader.FastSlotEvent += OnFastSlotInput;

        SetActiveSlot(activeSlot);
    }

    private void OnDisable()
    {
        lastSwappedEvent.OnRaised -= OnLastSwapped;
        lastRemovedEvent.OnRaised -= OnLastRemoved;
        activeSlotChangedEvent.OnRaised -= OnActiveSlotChanged;
        inventoryToggleEvent.OnRaised -= OnInventoryToggled;
        inputReader.FastSlotEvent -= OnFastSlotInput;
    }

    private void OnFastSlotInput(int inputPad)
    {
        if (isInventoryOpen)
            return;
        if (inputPad <= 0 || inputPad > FastSlotCount)
            return;
        equipRequestEvent.Raise(inputPad - 1);
    }

    private void OnLastRemoved((StorageType storageType, string itemId, int position) data)
    {
        if (data.storageType != StorageType.FastSlot)
            return;
        SetSlotItem(data.position, null);
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
            SetSlotItem(data.targetSlot, data.sourceItemId);
        if (data.sourceType == StorageType.FastSlot)
            SetSlotItem(data.sourceSlot, data.targetItemId);
    }

    private void OnActiveSlotChanged(int slotIndex)
    {
        SetActiveSlot(slotIndex);
    }

    private void OnInventoryToggled(bool status)
    {
        isInventoryOpen = status;
        for (int i = 0; i < slots.Count; i++)
        {
            if (i == activeSlot)
                SetSlotBackground(i, status ? hiddenHUDSlotSprite : selectedSlotSprite);
            else
                SetSlotBackground(i, status ? hiddenHUDSlotSprite : defaultSlotSprite);
        }
    }

    private void SetActiveSlot(int slotIndex)
    {
        SetSlotBackground(activeSlot, defaultSlotSprite);
        activeSlot = slotIndex;
        SetSlotBackground(activeSlot, selectedSlotSprite);
    }

    private void SetSlotItem(int index, string itemId)
    {
        var icon = Icon(index);
        if (icon == null)
            return;

        bool empty = string.IsNullOrEmpty(itemId);
        icon.style.backgroundImage = empty ? null : new StyleBackground(itemObtainedSprite);
        icon.style.unityBackgroundImageTintColor = Color.white;
    }

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
