using System.Collections.Generic;
using FTR.Core.Client.EventChannels.Status;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

[RequireComponent(typeof(UIDocument))]
public class InventoryUIController : MonoBehaviour
{
    private const int InventoryRows = 3;
    private const int InventoryColumns = 4;
    private const int InventorySlotCount = InventoryRows * InventoryColumns;

    [Inject]
    private LastItemChangedEvent lastItemChangedEvent;

    [Inject]
    private LastSwappedItemChangedEvent lastSwappedItemChangedEvent;

    [Inject]
    private LastDroppedItemChangedEvent lastDroppedItemChangedEvent;

    [Inject]
    private InventorySlotSwapRequestEvent swapRequestEvent;

    [Inject]
    private InventorySlotDropRequestEvent dropRequestEvent;

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

    private readonly List<VisualElement> slots = new List<VisualElement>(InventorySlotCount);
    private int selectedSlotIndex = -1;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        var root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null)
            return;

        slots.Clear();
        for (int i = 0; i < InventorySlotCount; i++)
        {
            var slot = root.Q<VisualElement>($"Slot{i + 1}");
            if (slot != null)
            {
                int capturedIndex = i; // capture for the closure
                slot.RegisterCallback<ClickEvent>(evt => OnSlotClicked(capturedIndex));
                slots.Add(slot);
            }
        }

        var dropButton = root.Q<VisualElement>("Drop");
        if (dropButton != null)
        {
            dropButton.RegisterCallback<ClickEvent>(evt => OnDropClicked());
        }

        inputReader.InventoryEvent += OnInventoryInput;

        if (lastItemChangedEvent != null)
            lastItemChangedEvent.OnRaised += OnLastItemChanged;

        if (lastSwappedItemChangedEvent != null)
            lastSwappedItemChangedEvent.OnRaised += OnLastSwappedItemChanged;

        if (lastDroppedItemChangedEvent != null)
            lastDroppedItemChangedEvent.OnRaised += OnLastDroppedItemChanged;
    }

    private void OnDisable()
    {
        inputReader.InventoryEvent -= OnInventoryInput;

        if (lastItemChangedEvent != null)
            lastItemChangedEvent.OnRaised -= OnLastItemChanged;

        if (lastSwappedItemChangedEvent != null)
            lastSwappedItemChangedEvent.OnRaised -= OnLastSwappedItemChanged;

        if (lastDroppedItemChangedEvent != null)
            lastDroppedItemChangedEvent.OnRaised -= OnLastDroppedItemChanged;
    }

    private void OnInventoryInput()
    {
        Debug.Log("Inventory input received. Toggling inventory UI.");
        var panel = uiDocument.rootVisualElement.Q<VisualElement>("Panel");

        if (panel != null)
        {
            panel.visible = !panel.visible;
            if (inventoryToggleEvent != null)
            {
                inventoryToggleEvent.Raise(panel.visible);
            }
        }
    }

    private void OnSlotClicked(int index)
    {
        if (selectedSlotIndex == -1)
        {
            selectedSlotIndex = index;
            UpdateSlotSprite(index, true);
        }
        else
        {
            if (selectedSlotIndex == index)
            {
                UpdateSlotSprite(index, false);
                selectedSlotIndex = -1;
            }
            else
            {
                if (swapRequestEvent != null)
                {
                    swapRequestEvent.Raise((selectedSlotIndex, index));
                    Debug.Log($"Requested swap from UI: {selectedSlotIndex} to {index}");
                }
                UpdateSlotSprite(selectedSlotIndex, false);
                selectedSlotIndex = -1;
            }
        }
    }

    private void OnDropClicked()
    {
        if (selectedSlotIndex != -1)
        {
            if (dropRequestEvent != null)
            {
                dropRequestEvent.Raise(selectedSlotIndex);
                Debug.Log($"Requested drop from UI for slot: {selectedSlotIndex}");
            }

            // Revert visual selection
            UpdateSlotSprite(selectedSlotIndex, false);
            selectedSlotIndex = -1;
        }
        else
        {
            Debug.Log("Drop clicked but no item is selected. Ignoring.");
        }
    }

    private void UpdateSlotSprite(int index, bool isSelected)
    {
        if (index < 0 || index >= slots.Count)
            return;
        var sprite = isSelected ? selectedSlotSprite : defaultSlotSprite;
        if (sprite != null)
        {
            slots[index].style.backgroundImage = new StyleBackground(sprite);
        }
    }

    private void OnLastItemChanged((string, int) data)
    {
        int slotNumber = ResolveSlotNumber(data.Item2);
        if (slotNumber < 1 || slotNumber > InventorySlotCount)
            return;

        Debug.Log($"Last item changed: {data.Item1} in slot {slotNumber}");

        ShowItemObtained(slotNumber, data.Item1);
    }

    private void OnLastDroppedItemChanged((string, int) data)
    {
        int slotNumber = ResolveSlotNumber(data.Item2);
        if (slotNumber < 1 || slotNumber > InventorySlotCount)
            return;

        Debug.Log($"Last item dropped: {data.Item1} from slot {slotNumber}");

        ShowItemObtained(slotNumber, string.Empty);
    }

    private void OnLastSwappedItemChanged((int source, int target) data)
    {
        Debug.Log($"Last swapped item changed: from {data.source} to {data.target}");

        int sourceSlotIndex = data.source;
        int targetSlotIndex = data.target;

        if (
            sourceSlotIndex < 0
            || sourceSlotIndex >= slots.Count
            || targetSlotIndex < 0
            || targetSlotIndex >= slots.Count
        )
            return;

        var sourceIcon = slots[sourceSlotIndex].Q<VisualElement>("ItemIcon");
        var targetIcon = slots[targetSlotIndex].Q<VisualElement>("ItemIcon");

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

    private int ResolveSlotNumber(int position)
    {
        if (position >= InventorySlotCount)
            return -1;

        int row = position / InventoryColumns;
        int column = position % InventoryColumns;
        int slotIndex = (row * InventoryColumns) + column;

        return slotIndex + 1;
    }

    private void ShowItemObtained(int slotNumber, string itemId)
    {
        int index = slotNumber - 1;
        if (index < 0 || index >= slots.Count || slots[index] == null)
            return;

        var slot = slots[index];
        var icon = slot.Q<VisualElement>("ItemIcon");

        if (icon != null)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                icon.style.backgroundImage = null;
                icon.style.unityBackgroundImageTintColor = Color.white;
                Debug.Log($"Cleared item from slot {slotNumber}");
            }
            else if (itemObtainedSprite != null)
            {
                icon.style.backgroundImage = new StyleBackground(itemObtainedSprite);
                icon.style.unityBackgroundImageTintColor = GetColorFromItemId(itemId);
                Debug.Log(
                    $"Showing item obtained in slot {slotNumber} with sprite {itemObtainedSprite.name}"
                );
            }
        }
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
