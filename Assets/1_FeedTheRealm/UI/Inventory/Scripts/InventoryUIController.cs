using System.Collections.Generic;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

[RequireComponent(typeof(UIDocument))]
public class InventoryUIController : MonoBehaviour
{
    private const int InventoryRows = 3;
    private const int InventoryColumns = 4;
    private const int InventorySlotCount = InventoryRows * InventoryColumns;
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
    private SlotEquipRequestEvent equipRequestEvent;

    [Inject]
    private SlotUnequipRequestEvent unequipRequestEvent;

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

    private readonly List<VisualElement> inventorySlots = new List<VisualElement>(
        InventorySlotCount
    );
    private readonly List<VisualElement> fastSlots = new List<VisualElement>(FastSlotCount);
    private int selectedSlotIndex = -1;
    private StorageType? selectedStorage;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        var root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null)
            return;

        inventorySlots.Clear();
        for (int i = 0; i < InventorySlotCount; i++)
        {
            var slot = root.Q<VisualElement>($"Slot{i + 1}");
            if (slot != null)
            {
                int capturedIndex = i; // capture for the closure
                slot.RegisterCallback<ClickEvent>(evt => OnInventorySlotClicked(capturedIndex));
                inventorySlots.Add(slot);
            }
        }

        fastSlots.Clear();
        for (int i = 0; i < FastSlotCount; i++)
        {
            var slot = root.Q<VisualElement>($"FastEquipSlot{i + 1}");
            if (slot != null)
            {
                int capturedIndex = i;
                slot.RegisterCallback<ClickEvent>(evt => OnFastSlotClicked(capturedIndex));
                fastSlots.Add(slot);
            }
        }

        var dropButton = root.Q<VisualElement>("Drop");
        if (dropButton != null)
        {
            dropButton.RegisterCallback<ClickEvent>(evt => OnDropClicked());
        }

        inputReader.InventoryEvent += OnInventoryInput;

        if (lastAddedEvent != null)
            lastAddedEvent.OnRaised += OnLastAdded;

        if (lastSwappedEvent != null)
            lastSwappedEvent.OnRaised += OnLastSwappedItemChanged;

        if (lastRemovedEvent != null)
            lastRemovedEvent.OnRaised += OnLastRemoved;
    }

    private void OnDisable()
    {
        inputReader.InventoryEvent -= OnInventoryInput;

        if (lastAddedEvent != null)
            lastAddedEvent.OnRaised -= OnLastAdded;

        if (lastSwappedEvent != null)
            lastSwappedEvent.OnRaised -= OnLastSwappedItemChanged;

        if (lastRemovedEvent != null)
            lastRemovedEvent.OnRaised -= OnLastRemoved;
    }

    private void OnInventoryInput()
    {
        Debug.Log("Inventory input received. Toggling inventory UI.");
        var inventory = uiDocument.rootVisualElement.Q<VisualElement>("Inventory");

        if (inventory != null)
        {
            bool isCurrentlyDisplayed = inventory.resolvedStyle.display != DisplayStyle.None;
            bool shouldDisplay = !isCurrentlyDisplayed;

            inventory.style.display = shouldDisplay ? DisplayStyle.Flex : DisplayStyle.None;
            inventoryToggleEvent?.Raise(shouldDisplay);
        }
    }

    private void OnInventorySlotClicked(int index)
    {
        HandleSlotClick(StorageType.Inventory, index);
    }

    private void OnFastSlotClicked(int index)
    {
        HandleSlotClick(StorageType.FastSlot, index);
    }

    private void HandleSlotClick(StorageType clickedStorage, int clickedIndex)
    {
        if (!selectedStorage.HasValue)
        {
            SetSelection(clickedStorage, clickedIndex);
            return;
        }

        if (selectedStorage == clickedStorage && selectedSlotIndex == clickedIndex)
        {
            ClearSelection();
            return;
        }

        RequestMoveOrSwap(selectedStorage.Value, selectedSlotIndex, clickedStorage, clickedIndex);
        ClearSelection();
    }

    private void SetSelection(StorageType storage, int index)
    {
        selectedStorage = storage;
        selectedSlotIndex = index;

        var targetSlots = storage == StorageType.FastSlot ? fastSlots : inventorySlots;
        UpdateSlotSprite(targetSlots, index, true);
    }

    private void RequestMoveOrSwap(
        StorageType fromStorage,
        int fromIndex,
        StorageType toStorage,
        int toIndex
    )
    {
        if (fromStorage == StorageType.Inventory && toStorage == StorageType.Inventory)
        {
            swapRequestEvent?.Raise((StorageType.Inventory, fromIndex, toIndex));
            Debug.Log($"Requested inventory swap from UI: {fromIndex} to {toIndex}");
            return;
        }

        if (fromStorage == StorageType.FastSlot && toStorage == StorageType.FastSlot)
        {
            swapRequestEvent?.Raise((StorageType.FastSlot, fromIndex, toIndex));
            Debug.Log($"Requested fast slot swap from inventory UI: {fromIndex} to {toIndex}");
            return;
        }

        if (fromStorage == StorageType.Inventory && toStorage == StorageType.FastSlot)
        {
            equipRequestEvent?.Raise((fromIndex, toIndex));
            Debug.Log($"Requested equip from inventory slot {fromIndex} to fast slot {toIndex}");
            return;
        }

        if (fromStorage == StorageType.FastSlot && toStorage == StorageType.Inventory)
        {
            unequipRequestEvent?.Raise((fromIndex, toIndex));
            Debug.Log($"Requested unequip from fast slot {fromIndex} to inventory slot {toIndex}");
        }
    }

    private void OnDropClicked()
    {
        if (selectedSlotIndex != -1)
        {
            if (dropRequestEvent != null)
            {
                var storageType =
                    selectedStorage == StorageType.FastSlot
                        ? StorageType.FastSlot
                        : StorageType.Inventory;

                dropRequestEvent.Raise((storageType, selectedSlotIndex));
                Debug.Log(
                    $"Requested drop from UI for storage {storageType} slot: {selectedSlotIndex}"
                );
            }

            ClearSelection();
        }
        else
        {
            Debug.Log("Drop clicked but no item is selected. Ignoring.");
        }
    }

    private void UpdateSlotSprite(List<VisualElement> targetSlots, int index, bool isSelected)
    {
        if (index < 0 || index >= targetSlots.Count)
            return;
        var sprite = isSelected ? selectedSlotSprite : defaultSlotSprite;
        if (sprite != null)
        {
            targetSlots[index].style.backgroundImage = new StyleBackground(sprite);
        }
    }

    private void ClearSelection()
    {
        if (selectedStorage == StorageType.Inventory)
            UpdateSlotSprite(inventorySlots, selectedSlotIndex, false);
        else if (selectedStorage == StorageType.FastSlot)
            UpdateSlotSprite(fastSlots, selectedSlotIndex, false);

        selectedStorage = null;
        selectedSlotIndex = -1;
    }

    private void OnLastAdded((StorageType, string, int) data)
    {
        ShowItemObtained(data.Item1, data.Item3, data.Item2);
    }

    private void OnLastRemoved((StorageType, string, int) data)
    {
        ShowItemObtained(data.Item1, data.Item3, string.Empty);
    }

    private void OnLastSwappedItemChanged((StorageType, int, int) data)
    {
        var targetSlots = data.Item1 == StorageType.FastSlot ? fastSlots : inventorySlots;
        SwapSlotVisuals(data.Item1, targetSlots, data.Item2, data.Item3);
    }

    private void SwapSlotVisuals(
        StorageType storageType,
        List<VisualElement> targetSlots,
        int sourceSlotIndex,
        int targetSlotIndex
    )
    {
        if (
            sourceSlotIndex < 0
            || sourceSlotIndex >= targetSlots.Count
            || targetSlotIndex < 0
            || targetSlotIndex >= targetSlots.Count
        )
            return;

        var sourceIcon = GetSlotIcon(storageType, targetSlots[sourceSlotIndex]);
        var targetIcon = GetSlotIcon(storageType, targetSlots[targetSlotIndex]);

        if (sourceIcon != null && targetIcon != null)
        {
            var tempBackground = sourceIcon.style.backgroundImage;
            var tempTint = sourceIcon.style.unityBackgroundImageTintColor;

            sourceIcon.style.backgroundImage = targetIcon.style.backgroundImage;
            sourceIcon.style.unityBackgroundImageTintColor = targetIcon
                .style
                .unityBackgroundImageTintColor;

            targetIcon.style.backgroundImage = tempBackground;
            targetIcon.style.unityBackgroundImageTintColor = tempTint;
            Debug.Log($"Swapped backgrounds between slot {sourceSlotIndex} and {targetSlotIndex}");
        }
    }

    private void ShowItemObtained(StorageType storageType, int position, string itemId)
    {
        var targetSlots = storageType == StorageType.FastSlot ? fastSlots : inventorySlots;
        if (position < 0 || position >= targetSlots.Count || targetSlots[position] == null)
            return;

        var slot = targetSlots[position];
        var icon = GetSlotIcon(storageType, slot);

        if (icon != null)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                icon.style.backgroundImage = null;
                icon.style.unityBackgroundImageTintColor = Color.white;
                Debug.Log($"Cleared item from {storageType} slot {position}");
            }
            else if (itemObtainedSprite != null)
            {
                icon.style.backgroundImage = new StyleBackground(itemObtainedSprite);
                icon.style.unityBackgroundImageTintColor = GetColorFromItemId(itemId);
                Debug.Log($"Showing item obtained in {storageType} slot {position}");
            }
        }
    }

    private VisualElement GetSlotIcon(StorageType storageType, VisualElement slot)
    {
        if (slot == null)
            return null;

        if (storageType == StorageType.FastSlot)
            return slot.Q<VisualElement>("FastEquipIcon") ?? slot;

        return slot.Q<VisualElement>("ItemIcon") ?? slot;
    }

    private Color GetColorFromItemId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return Color.white;
        var rand = new System.Random(System.DateTime.Now.Millisecond);

        return new Color(
            (float)rand.NextDouble() * 0.7f + 0.3f,
            (float)rand.NextDouble() * 0.7f + 0.3f,
            (float)rand.NextDouble() * 0.7f + 0.3f,
            1f
        );
    }
}
