using FTR.Core.Client.EventChannels.Status;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InventoryUIController : MonoBehaviour
{
    private const int InventoryRows = 3;
    private const int InventoryColumns = 4;
    private const int InventorySlotCount = InventoryRows * InventoryColumns;

    [SerializeField]
    private LastItemChangedEvent lastItemChangedEvent;

    private UIDocument uiDocument;
    private readonly VisualElement[] slots = new VisualElement[InventorySlotCount];

    private void OnEnable()
    {
        CacheSlots();

        if (lastItemChangedEvent != null)
            lastItemChangedEvent.OnRaised += OnLastItemChanged;
    }

    private void OnDisable()
    {
        if (lastItemChangedEvent != null)
            lastItemChangedEvent.OnRaised -= OnLastItemChanged;
    }

    private void OnLastItemChanged((string, int) data)
    {
        int slotNumber = ResolveSlotNumber(data.Item2);
        if (slotNumber < 1 || slotNumber > InventorySlotCount)
            return;

        HighlightSlot(slotNumber);
    }

    private void CacheSlots()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        var root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null)
            return;

        for (int i = 0; i < InventorySlotCount; i++)
        {
            slots[i] = root.Q<VisualElement>($"Slot{i + 1}");
        }
    }

    private void HighlightSlot(int slotNumber)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            bool isSelected = i == slotNumber - 1;
            slots[i].style.borderTopWidth = isSelected ? 3f : 1f;
            slots[i].style.borderRightWidth = isSelected ? 3f : 1f;
            slots[i].style.borderBottomWidth = isSelected ? 3f : 1f;
            slots[i].style.borderLeftWidth = isSelected ? 3f : 1f;
            slots[i].style.borderTopColor = isSelected ? Color.yellow : new Color(1f, 1f, 1f, 0.5f);
            slots[i].style.borderRightColor = isSelected
                ? Color.yellow
                : new Color(1f, 1f, 1f, 0.5f);
            slots[i].style.borderBottomColor = isSelected
                ? Color.yellow
                : new Color(1f, 1f, 1f, 0.5f);
            slots[i].style.borderLeftColor = isSelected
                ? Color.yellow
                : new Color(1f, 1f, 1f, 0.5f);
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
}
