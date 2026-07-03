using System.Collections;
using System.Collections.Generic;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.Hud.Main
{
    [RequireComponent(typeof(UIDocument))]
    public class FastSlotCooldownView : MonoBehaviour
    {
        private const int FastSlotCount = 5;

        [Inject]
        private CooldownStartedEvent cooldownStartedEvent;

        [Inject]
        private ActiveSlotChangedEvent activeSlotChangedEvent;

        [Inject]
        private LastAddedEvent lastAddedEvent;

        [Inject]
        private LastSwappedEvent lastSwappedEvent;

        [Inject]
        private LastRemovedEvent lastRemovedEvent;

        private readonly VisualElement[] slots = new VisualElement[FastSlotCount];
        private readonly Coroutine[] slotCoroutines = new Coroutine[FastSlotCount];
        private readonly string[] slotItemIds = new string[FastSlotCount];

        private readonly Dictionary<string, float> itemCooldownEndTimes = new();

        private int activeSlot;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>()?.rootVisualElement;
            if (root != null)
                for (int i = 0; i < FastSlotCount; i++)
                    slots[i] = root.Q($"FastEquipSlot{i + 1}");

            cooldownStartedEvent.OnRaised += OnCooldownStarted;
            activeSlotChangedEvent.OnRaised += OnActiveSlotChanged;
            lastAddedEvent.OnRaised += OnLastAdded;
            lastSwappedEvent.OnRaised += OnLastSwapped;
            lastRemovedEvent.OnRaised += OnLastRemoved;
        }

        private void OnDisable()
        {
            cooldownStartedEvent.OnRaised -= OnCooldownStarted;
            activeSlotChangedEvent.OnRaised -= OnActiveSlotChanged;
            lastAddedEvent.OnRaised -= OnLastAdded;
            lastSwappedEvent.OnRaised -= OnLastSwapped;
            lastRemovedEvent.OnRaised -= OnLastRemoved;

            for (int i = 0; i < FastSlotCount; i++)
                CancelSlot(i);
        }

        private void OnLastAdded((StorageType type, string id, int pos, int qty) data)
        {
            if (data.type != StorageType.FastSlot || data.pos >= FastSlotCount)
                return;

            slotItemIds[data.pos] = data.id;
            TryReapplyCooldown(data.pos, data.id);
        }

        private void OnLastRemoved((StorageType type, string id, int pos) data)
        {
            if (data.type != StorageType.FastSlot || data.pos >= FastSlotCount)
                return;

            slotItemIds[data.pos] = null;
            CancelSlot(data.pos);
        }

        private void OnLastSwapped(
            (
                StorageType srcT,
                int srcI,
                string srcId,
                int srcQty,
                StorageType tgtT,
                int tgtI,
                string tgtId,
                int tgtQty
            ) data
        )
        {
            if (data.srcT == StorageType.FastSlot && data.srcI < FastSlotCount)
            {
                slotItemIds[data.srcI] = data.tgtId;
                CancelSlot(data.srcI);
                TryReapplyCooldown(data.srcI, data.tgtId);
            }

            if (data.tgtT == StorageType.FastSlot && data.tgtI < FastSlotCount)
            {
                slotItemIds[data.tgtI] = data.srcId;
                CancelSlot(data.tgtI);
                TryReapplyCooldown(data.tgtI, data.srcId);
            }
        }

        private void TryReapplyCooldown(int slotIndex, string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return;
            if (!itemCooldownEndTimes.TryGetValue(itemId, out float endTime))
                return;

            float remaining = endTime - Time.time;
            if (remaining > 0f)
                StartSlotCooldown(slotIndex, remaining);
        }

        private void OnActiveSlotChanged(int index) => activeSlot = index;

        private void OnCooldownStarted((string itemId, float duration) data)
        {
            itemCooldownEndTimes[data.itemId] = Time.time + data.duration;
            for (int i = 0; i < FastSlotCount; i++)
                if (slotItemIds[i] == data.itemId)
                    StartSlotCooldown(i, data.duration);
        }

        private void StartSlotCooldown(int index, float duration)
        {
            if (index < 0 || index >= FastSlotCount)
                return;
            var slot = slots[index];
            if (slot == null)
                return;

            var overlay = slot.Q("CooldownOverlay");
            var bar = slot.Q("CooldownBar");
            var icon = slot.Q("FastEquipIcon");

            if (overlay == null || bar == null)
                return;

            CancelSlot(index);

            icon?.AddToClassList("slot-icon--cooldown");
            bar.style.width = Length.Percent(0f);
            overlay.style.display = DisplayStyle.Flex;

            slotCoroutines[index] = StartCoroutine(CooldownCoroutine(overlay, bar, icon, duration));
        }

        private IEnumerator CooldownCoroutine(
            VisualElement overlay,
            VisualElement bar,
            VisualElement icon,
            float duration
        )
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bar.style.width = Length.Percent(Mathf.Clamp01(elapsed / duration) * 100f);
                yield return null;
            }
            overlay.style.display = DisplayStyle.None;
            icon?.RemoveFromClassList("slot-icon--cooldown");
        }

        private void CancelSlot(int index)
        {
            if (slotCoroutines[index] == null)
                return;
            StopCoroutine(slotCoroutines[index]);
            slotCoroutines[index] = null;

            var slot = slots[index];
            if (slot == null)
                return;
            var overlay = slot.Q("CooldownOverlay");
            if (overlay != null)
                overlay.style.display = DisplayStyle.None;
            slot.Q("FastEquipIcon")?.RemoveFromClassList("slot-icon--cooldown");
        }
    }
}
