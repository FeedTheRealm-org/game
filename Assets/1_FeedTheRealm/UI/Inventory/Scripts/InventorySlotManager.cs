using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Handles inventory slot initialization and management.
/// </summary>
public class InventorySlotManager
{
    private List<VisualElement> slots = new List<VisualElement>();
    private VisualElement root;
    private Sprite slotNormalSprite;
    private Sprite slotHoverSprite;

    public InventorySlotManager(VisualElement root, Sprite normalSprite, Sprite hoverSprite)
    {
        this.root = root;
        this.slotNormalSprite = normalSprite;
        this.slotHoverSprite = hoverSprite;
    }

    public List<VisualElement> InitializeSlots(
        int slotCount,
        System.Action<PointerDownEvent, VisualElement> onPointerDown,
        System.Action<PointerUpEvent, VisualElement> onPointerUp,
        System.Action<PointerEnterEvent, VisualElement> onPointerEnter,
        System.Action<PointerLeaveEvent, VisualElement> onPointerLeave
    )
    {
        slots.Clear();
        for (int i = 1; i <= slotCount; i++)
        {
            var slot = root.Q<VisualElement>($"Slot{i}");
            if (slot != null)
            {
                slots.Add(slot);
                slot.RegisterCallback<PointerDownEvent>(evt => onPointerDown(evt, slot));
                slot.RegisterCallback<PointerUpEvent>(evt => onPointerUp(evt, slot));
                slot.RegisterCallback<PointerEnterEvent>(evt => onPointerEnter(evt, slot));
                slot.RegisterCallback<PointerLeaveEvent>(evt => onPointerLeave(evt, slot));
                if (slotNormalSprite != null && slotHoverSprite != null)
                {
                    slot.RegisterCallback<PointerEnterEvent>(evt =>
                        slot.style.backgroundImage = new StyleBackground(slotHoverSprite)
                    );
                    slot.RegisterCallback<PointerLeaveEvent>(evt =>
                        slot.style.backgroundImage = new StyleBackground(slotNormalSprite)
                    );
                }
            }
        }
        return slots;
    }
}
