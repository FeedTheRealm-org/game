using System.Collections.Generic;
using UnityEngine;

namespace FTR.Gameplay.Server.Utils.UseEquipment
{
    /// <summary>
    /// Tracks use cooldowns for fast slots and consumable item types independently.
    /// </summary>
    public class SlotCooldownTracker
    {
        private readonly float[] lastUsedTime;
        private readonly float[] cooldownDuration;
        private readonly Dictionary<string, float> consumableExpiry = new();

        public SlotCooldownTracker(int slotCount)
        {
            lastUsedTime = new float[slotCount];
            cooldownDuration = new float[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                lastUsedTime[i] = float.NegativeInfinity;
                cooldownDuration[i] = 0f;
            }
        }

        public bool IsSlotReady(int slot, out float remaining)
        {
            float elapsed = Time.time - lastUsedTime[slot];
            remaining = Mathf.Max(0f, cooldownDuration[slot] - elapsed);
            return remaining <= 0f;
        }

        public void RecordSlotUsed(int slot, float cooldown)
        {
            lastUsedTime[slot] = Time.time;
            cooldownDuration[slot] = cooldown;
        }

        public bool IsConsumableCoolingDown(string itemId, out float remaining)
        {
            if (consumableExpiry.TryGetValue(itemId, out float expiry))
            {
                remaining = Mathf.Max(0f, expiry - Time.time);
                return remaining > 0f;
            }

            remaining = 0f;
            return false;
        }

        public void RecordConsumableUsed(string itemId, float cooldown)
        {
            consumableExpiry[itemId] = Time.time + cooldown;
        }
    }
}
