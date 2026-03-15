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

    [SerializeField]
    private Sprite defaultSlotSprite;

    [SerializeField]
    private Sprite itemObtainedSprite;

    private UIDocument uiDocument;

    private readonly List<VisualElement> slots = new List<VisualElement>(FastSlotCount);

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        var root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null)
            return;

        slots.Clear();
        for (int i = 0; i < FastSlotCount; i++)
        {
            var slot =
                root.Q<VisualElement>($"FastEquipSlot{i + 1}")
                ?? root.Q<VisualElement>($"Slot{i + 1}");
            if (slot != null)
            {
                if (defaultSlotSprite != null)
                    slot.style.backgroundImage = new StyleBackground(defaultSlotSprite);

                slots.Add(slot);
            }
        }

        if (lastAddedEvent != null)
            lastAddedEvent.OnRaised += OnLastAdded;

        if (lastSwappedEvent != null)
            lastSwappedEvent.OnRaised += OnLastSwappedItemChanged;

        if (lastRemovedEvent != null)
            lastRemovedEvent.OnRaised += OnLastDroppedItemChanged;
    }

    private void OnDisable()
    {
        if (lastAddedEvent != null)
            lastAddedEvent.OnRaised -= OnLastAdded;

        if (lastSwappedEvent != null)
            lastSwappedEvent.OnRaised -= OnLastSwappedItemChanged;

        if (lastRemovedEvent != null)
            lastRemovedEvent.OnRaised -= OnLastDroppedItemChanged;
    }

    private void OnLastAdded((StorageType, string, int) data)
    {
        if (data.Item1 != StorageType.FastSlot)
            return;

        int slotNumber = ResolveSlotNumber(data.Item3);
        if (slotNumber < 1 || slotNumber > FastSlotCount)
            return;

        Debug.Log($"Fast slot item changed: {data.Item2} in slot {slotNumber}");
        ShowItemObtained(slotNumber, data.Item2);
    }

    private void OnLastDroppedItemChanged((StorageType, string, int) data)
    {
        if (data.Item1 != StorageType.FastSlot)
            return;

        int slotNumber = ResolveSlotNumber(data.Item3);
        if (slotNumber < 1 || slotNumber > FastSlotCount)
            return;

        Debug.Log($"Fast slot item dropped: {data.Item2} from slot {slotNumber}");
        ShowItemObtained(slotNumber, string.Empty);
    }

    private void OnLastSwappedItemChanged((StorageType, int, int) data)
    {
        if (data.Item1 != StorageType.FastSlot)
            return;

        int sourceSlotIndex = data.Item2;
        int targetSlotIndex = data.Item3;

        if (
            sourceSlotIndex < 0
            || sourceSlotIndex >= slots.Count
            || targetSlotIndex < 0
            || targetSlotIndex >= slots.Count
        )
            return;

        var sourceIcon = GetSlotIcon(slots[sourceSlotIndex]);
        var targetIcon = GetSlotIcon(slots[targetSlotIndex]);

        if (sourceIcon == null || targetIcon == null)
            return;

        var tempBackground = sourceIcon.style.backgroundImage;
        var tempTint = sourceIcon.style.unityBackgroundImageTintColor;

        sourceIcon.style.backgroundImage = targetIcon.style.backgroundImage;
        sourceIcon.style.unityBackgroundImageTintColor = targetIcon
            .style
            .unityBackgroundImageTintColor;

        targetIcon.style.backgroundImage = tempBackground;
        targetIcon.style.unityBackgroundImageTintColor = tempTint;

        Debug.Log($"Swapped fast slot visuals between {sourceSlotIndex} and {targetSlotIndex}");
    }

    private int ResolveSlotNumber(int position)
    {
        if (position < 0 || position >= FastSlotCount)
            return -1;

        return position + 1;
    }

    private void ShowItemObtained(int slotNumber, string itemId)
    {
        int index = slotNumber - 1;
        if (index < 0 || index >= slots.Count || slots[index] == null)
            return;

        var slot = slots[index];
        var icon = GetSlotIcon(slot);

        if (icon == null)
            return;

        if (string.IsNullOrEmpty(itemId))
        {
            icon.style.backgroundImage = null;
            icon.style.backgroundColor = Color.clear;
            icon.style.unityBackgroundImageTintColor = Color.white;
            Debug.Log($"Cleared item from fast slot {slotNumber}");
        }
        else
        {
            if (itemObtainedSprite != null)
                icon.style.backgroundImage = new StyleBackground(itemObtainedSprite);
            else
                icon.style.backgroundImage = null;

            icon.style.backgroundColor = Color.clear;
            icon.style.unityBackgroundImageTintColor = Color.white;
            Debug.Log($"Showing fast slot item in slot {slotNumber}");
        }
    }

    private VisualElement GetSlotIcon(VisualElement slot)
    {
        if (slot == null)
            return null;

        return slot.Q<VisualElement>("FastEquipIcon") ?? slot.Q<VisualElement>("ItemIcon") ?? slot;
    }
}
