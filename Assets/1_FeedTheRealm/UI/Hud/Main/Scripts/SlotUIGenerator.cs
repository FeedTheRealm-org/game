using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeedTheRealm.UI.Hud
{
    /// <summary>
    /// Utility for dynamically generating slot UI elements at runtime.
    /// Supports both container-style slots (VisualElement) and button-style slots.
    /// </summary>
    public static class SlotUIGenerator
    {
        /// <summary>
        /// Generates button-style slots (for HUD quickslots).
        /// </summary>
        /// <param name="container">Parent container where buttons will be added</param>
        /// <param name="slotCount">Number of slots to generate</param>
        /// <param name="slotNamePrefix">Prefix for slot names (e.g., "Slot")</param>
        /// <param name="useOneBasedNaming">If true, names start at 1 (Slot1, Slot2). If false, start at 0</param>
        /// <param name="cssClass">CSS class to apply to each button (e.g., "fast-use-slot")</param>
        /// <param name="focusable">Whether buttons should be focusable</param>
        /// <returns>Dictionary mapping slot index (1-based or 0-based) to Button elements</returns>
        public static Dictionary<int, Button> GenerateButtonSlots(
            VisualElement container,
            int slotCount,
            string slotNamePrefix = "Slot",
            bool useOneBasedNaming = true,
            string cssClass = null,
            bool focusable = false
        )
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            var slots = new Dictionary<int, Button>();
            int startIndex = useOneBasedNaming ? 1 : 0;

            for (int i = 0; i < slotCount; i++)
            {
                int slotIndex = startIndex + i;
                string slotName = $"{slotNamePrefix}{slotIndex}";

                var button = new Button { name = slotName, focusable = focusable };

                button.SetEnabled(true);

                // Apply CSS class if provided
                if (!string.IsNullOrEmpty(cssClass))
                {
                    button.AddToClassList(cssClass);
                }

                container.Add(button);
                slots[slotIndex] = button;
            }

            return slots;
        }

        /// <summary>
        /// Generates container-style slots (for inventory grids).
        /// </summary>
        /// <param name="container">Parent container where slots will be added</param>
        /// <param name="slotCount">Number of slots to generate</param>
        /// <param name="slotNamePrefix">Prefix for slot names (e.g., "Slot")</param>
        /// <param name="useOneBasedNaming">If true, names start at 1 (Slot1, Slot2). If false, start at 0</param>
        /// <param name="cssClass">CSS class to apply to each slot (e.g., "inventory-slot")</param>
        /// <returns>List of generated VisualElement slots</returns>
        public static List<VisualElement> GenerateContainerSlots(
            VisualElement container,
            int slotCount,
            string slotNamePrefix = "Slot",
            bool useOneBasedNaming = true,
            string cssClass = null
        )
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            var slots = new List<VisualElement>();
            int startIndex = useOneBasedNaming ? 1 : 0;

            for (int i = 0; i < slotCount; i++)
            {
                int slotIndex = startIndex + i;
                string slotName = $"{slotNamePrefix}{slotIndex}";

                var slot = new VisualElement { name = slotName };

                // Apply CSS class if provided
                if (!string.IsNullOrEmpty(cssClass))
                {
                    slot.AddToClassList(cssClass);
                }

                container.Add(slot);
                slots.Add(slot);
            }

            return slots;
        }

        /// <summary>
        /// Clears all children from a container.
        /// Useful for regenerating slots when configuration changes.
        /// </summary>
        public static void ClearContainer(VisualElement container)
        {
            if (container == null)
                return;

            container.Clear();
        }
    }
}
