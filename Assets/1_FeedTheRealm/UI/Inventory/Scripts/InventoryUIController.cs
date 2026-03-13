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

    [SerializeField]
    private PlayerInputReader inputReader;

    [SerializeField]
    private Sprite itemObtainedSprite;

    private UIDocument uiDocument;

    private readonly List<VisualElement> slots = new List<VisualElement>(InventorySlotCount);

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
            slots.Add(root.Q<VisualElement>($"Slot{i + 1}"));
        }

        inputReader.InventoryEvent += OnInventoryInput;

        if (lastItemChangedEvent != null)
            lastItemChangedEvent.OnRaised += OnLastItemChanged;
    }

    private void OnDisable()
    {
        inputReader.InventoryEvent -= OnInventoryInput;

        if (lastItemChangedEvent != null)
            lastItemChangedEvent.OnRaised -= OnLastItemChanged;
    }

    private void OnInventoryInput()
    {
        Debug.Log("Inventory input received. Toggling inventory UI.");
        var panel = uiDocument.rootVisualElement.Q<VisualElement>("Panel");

        if (panel != null)
            panel.visible = !panel.visible;
    }

    private void OnLastItemChanged((string, int) data)
    {
        int slotNumber = ResolveSlotNumber(data.Item2);
        if (slotNumber < 1 || slotNumber > InventorySlotCount)
            return;

        ShowItemObtained(slotNumber);
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

    private void ShowItemObtained(int slotNumber)
    {
        if (itemObtainedSprite == null)
            return;

        int index = slotNumber - 1;
        if (index < 0 || index >= slots.Count || slots[index] == null)
            return;

        var slot = slots[index];
        var icon = slot.Q<VisualElement>("ItemIcon");

        Debug.Log(
            $"Showing item obtained in slot {slotNumber} with sprite {itemObtainedSprite.name}"
        );

        if (icon == null)
        {
            icon = new VisualElement { name = "ItemIcon" };

            // Setting position absolute to fill the parent slot correctly
            icon.style.position = Position.Absolute;
            icon.style.top = 0;
            icon.style.bottom = 0;
            icon.style.left = 0;
            icon.style.right = 0;
            icon.style.width = Length.Pixels(200);
            icon.style.height = Length.Pixels(200);

            slot.Add(icon);
        }

        icon.style.backgroundImage = new StyleBackground(itemObtainedSprite);
        icon.style.display = DisplayStyle.Flex;
    }
}
