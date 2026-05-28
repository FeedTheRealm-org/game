using System.Collections.Generic;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Cache;
using FTR.UI.Inventory;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.Hud.Main
{
    [RequireComponent(typeof(UIDocument))]
    public class FastSlotUIController : MonoBehaviour
    {
        private const int FastSlotCount = 5;

        [Inject]
        private WorldSelector worldSelector;

        [Inject]
        private LastAddedEvent lastAddedEvent;

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
        private API.ItemAssetsService itemAssetsService;

        [Inject]
        private CacheManager cacheManager;

        private const string FastSlotSelectedClass = "fast-equip-slot--selected";
        private const string FastSlotHiddenClass = "fast-equip-slot--hidden";

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
                if (slot != null)
                    slots.Add(slot);
            }

            lastAddedEvent.OnRaised += OnLastAdded;
            lastSwappedEvent.OnRaised += OnLastSwapped;
            lastRemovedEvent.OnRaised += OnLastRemoved;
            activeSlotChangedEvent.OnRaised += OnActiveSlotChanged;
            inventoryToggleEvent.OnRaised += OnInventoryToggled;
            inputReader.FastSlotEvent += OnFastSlotInput;

            SetActiveSlot(activeSlot);
        }

        private void OnDisable()
        {
            lastAddedEvent.OnRaised -= OnLastAdded;
            lastSwappedEvent.OnRaised -= OnLastSwapped;
            lastRemovedEvent.OnRaised -= OnLastRemoved;
            activeSlotChangedEvent.OnRaised -= OnActiveSlotChanged;
            inventoryToggleEvent.OnRaised -= OnInventoryToggled;
            inputReader.FastSlotEvent -= OnFastSlotInput;
        }

        // ────────────────────────── Input ─────────────────────────────────────────────

        private void OnFastSlotInput(int inputPad)
        {
            if (isInventoryOpen)
                return;
            if (inputPad <= 0 || inputPad > FastSlotCount)
                return;
            equipRequestEvent.Raise(inputPad - 1);
        }

        // ────────────────────────── Inventory events ─────────────────────────────────────
        private void OnLastAdded(
            (StorageType storageType, string itemId, int position, int quantity) data
        )
        {
            if (data.storageType != StorageType.FastSlot)
                return;
            SlotItemLoader.LoadItem(
                Icon(data.position),
                data.itemId,
                cacheManager,
                worldSelector.GetSelectedWorldId(),
                data.quantity
            );
        }

        private void OnLastRemoved((StorageType storageType, string itemId, int position) data)
        {
            if (data.storageType != StorageType.FastSlot)
                return;
            SlotItemLoader.LoadItem(Icon(data.position), null, cacheManager);
        }

        private void OnLastSwapped(
            (
                StorageType sourceType,
                int sourceSlot,
                string sourceItemId,
                int sourceQuantity,
                StorageType targetType,
                int targetSlot,
                string targetItemId,
                int targetQuantity
            ) data
        )
        {
            if (data.targetType == StorageType.FastSlot)
                SlotItemLoader.LoadItem(
                    Icon(data.targetSlot),
                    data.sourceItemId,
                    cacheManager,
                    worldSelector.GetSelectedWorldId(),
                    data.sourceQuantity
                );
            if (data.sourceType == StorageType.FastSlot)
                SlotItemLoader.LoadItem(
                    Icon(data.sourceSlot),
                    data.targetItemId,
                    cacheManager,
                    worldSelector.GetSelectedWorldId(),
                    data.targetQuantity
                );
        }

        // ────────────────────────── Active slot & visibility ──────────────────────────────────

        private void OnActiveSlotChanged(int slotIndex) => SetActiveSlot(slotIndex);

        private void OnInventoryToggled(bool status)
        {
            isInventoryOpen = status;
            UpdateAllSlots();
        }

        private void SetActiveSlot(int slotIndex)
        {
            activeSlot = slotIndex;
            UpdateAllSlots();
        }

        // ────────────────────────── Helpers ──────────────────────────────────────

        private void UpdateAllSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                slot.RemoveFromClassList(FastSlotSelectedClass);
                slot.RemoveFromClassList(FastSlotHiddenClass);

                if (isInventoryOpen)
                    slot.AddToClassList(FastSlotHiddenClass);
                else if (i == activeSlot)
                    slot.AddToClassList(FastSlotSelectedClass);
            }
        }

        private VisualElement Icon(int index)
        {
            if (index < 0 || index >= slots.Count)
                return null;
            return slots[index].Q("FastEquipIcon") ?? slots[index];
        }
    }
}
