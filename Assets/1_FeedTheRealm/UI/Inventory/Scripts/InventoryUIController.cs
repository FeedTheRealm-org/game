using System;
using System.Collections.Generic;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Client.EventChannels.Input;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Client.Managers;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Cache;
using FTR.Gameplay.Client.Registry;
using FTR.UI.Inventory;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.Inventory
{
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(AnimationInventoryUIController))]
    public class InventoryUIController : MonoBehaviour
    {
        // ─── Fixed layout — matches UXML exactly ─────────────────────────────
        private const int InventorySlotCount = 35; // 7 rows × 5 cols
        private const int FastSlotCount = 5;
        private const int InventoryColumns = 5;
        private const float SlotSize = 70f; // must match .inventory-slot width in USS
        private const float SlotGap = 6f; // must match margin×2 in USS (3px/side)

        // ─── CSS class names ──────────────────────────────────────────────────
        // Images are defined entirely in Inventory.uss.
        // The controller only swaps these classes — it never sets backgroundImage.
        //
        //   .inventory-slot              → default inventory slot image
        //   .inventory-slot--selected    → selected inventory slot image
        //   .fast-equip-slot             → default fast-equip slot image
        //   .fast-equip-slot--selected   → selected fast-equip slot image
        //
        private const string InvSlotClass = "inventory-slot";
        private const string InvSlotSelectedClass = "inventory-slot--selected";
        private const string FastSlotClass = "fast-equip-slot";
        private const string FastSlotSelectedClass = "fast-equip-slot--selected";

        // ─────────────────────────────────────────────────────────────────────

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
        private InventoryToggleEvent inventoryToggleEvent;

        [Inject]
        private WorldSelector worldSelector;

        [Inject]
        private ISoundPlayer soundPlayer;

        [Inject]
        private MenuManager menuManager;

        [SerializeField]
        private PlayerInputReader inputReader;

        [Inject]
        private CacheManager cacheManager;

        [SerializeField]
        private ItemStatsTooltip itemStatsTooltipPrefab;

        private UIDocument uiDocument;
        private AnimationInventoryUIController animationController;
        private ItemStatsTooltip itemStatsTooltip;

        private readonly List<VisualElement> inventorySlots = new(InventorySlotCount);
        private readonly List<VisualElement> fastSlots = new(FastSlotCount);
        private readonly Dictionary<VisualElement, string> slotItemIds = new();

        private int selectedSlotIndex = -1;
        private StorageType selectedStorage;

        private readonly InventorySlotGhostController ghost = new();

        // ─── Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            animationController = GetComponent<AnimationInventoryUIController>();

            if (itemStatsTooltip == null && itemStatsTooltipPrefab != null)
                itemStatsTooltip = Instantiate(itemStatsTooltipPrefab);

            var root = uiDocument.rootVisualElement;
            if (root == null)
                return;

            animationController.Initialize(root);
            ApplyGridWidth(root);

            RegisterSlots(
                root,
                inventorySlots,
                InventorySlotCount,
                "Slot",
                OnInventorySlotClicked,
                StorageType.Inventory
            );

            RegisterSlots(
                root,
                fastSlots,
                FastSlotCount,
                "FastEquipSlot",
                OnFastSlotClicked,
                StorageType.FastSlot
            );

            root.Q("Drop")?.RegisterCallback<ClickEvent>(_ => OnDropClicked());

            inputReader.InventoryEvent += OnToggleInventory;
            lastAddedEvent.OnRaised += OnLastAdded;
            lastSwappedEvent.OnRaised += OnLastSwapped;
            lastRemovedEvent.OnRaised += OnLastRemoved;
            menuManager.RegisterMenuCallbacks(
                MenuType.Inventory,
                onOpen: null,
                onClose: CloseInventory
            );
        }

        private void OnDisable()
        {
            inputReader.InventoryEvent -= OnToggleInventory;
            lastAddedEvent.OnRaised -= OnLastAdded;
            lastSwappedEvent.OnRaised -= OnLastSwapped;
            lastRemovedEvent.OnRaised -= OnLastRemoved;
        }

        // ─── Setup ───────────────────────────────────────────────────────────

        private void ApplyGridWidth(VisualElement root)
        {
            var grid = root.Q("InventoryGrid");
            if (grid == null)
                return;

            float width = InventoryColumns * (SlotSize + SlotGap);
            grid.style.width = new StyleLength(width);
        }

        private void RegisterSlots(
            VisualElement root,
            List<VisualElement> list,
            int count,
            string prefix,
            Action<int> onClick,
            StorageType storage
        )
        {
            list.Clear();
            for (int i = 0; i < count; i++)
            {
                var slot = root.Q($"{prefix}{i + 1}");
                if (slot == null)
                    continue;

                // The slot already has its default class (.inventory-slot or
                // .fast-equip-slot) from the UXML, which contains the background-image
                // in the USS. We do NOT set backgroundImage here so the USS image
                // always shows without any C# override.

                int idx = i;
                slot.RegisterCallback<ClickEvent>(_ => onClick(idx));

                slot.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    ghost.OnHoverEnter(selectedStorage, selectedSlotIndex, storage, idx, Icon);
                    if (slotItemIds.TryGetValue(slot, out var itemId))
                        itemStatsTooltip?.ShowTooltip(itemId, slot);
                });

                slot.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    ghost.OnHoverLeave();
                    itemStatsTooltip?.HideTooltip();
                });

                list.Add(slot);
            }
        }

        // ─── Toggle / close ──────────────────────────────────────────────────

        private void OnToggleInventory()
        {
            bool show = !animationController.IsVisible;
            if (show && !menuManager.CanOpenMenu(MenuType.Inventory))
                return;

            animationController.Toggle();
            inventoryToggleEvent?.Raise(show);
            soundPlayer.PlayUI(
                show
                    ? ClientSoundFXRegistry.SoundFXIds.OpenUI
                    : ClientSoundFXRegistry.SoundFXIds.CloseUI
            );

            menuManager.ToggleMenu(MenuType.Inventory, show);
        }

        private void CloseInventory()
        {
            if (!animationController.IsVisible)
                return;
            animationController.Toggle();
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.CloseUI);
            inventoryToggleEvent?.Raise(false);
            menuManager.ToggleMenu(MenuType.Inventory, false);
        }

        // ─── Slot interaction ────────────────────────────────────────────────

        private void OnInventorySlotClicked(int i) => HandleSlotClick(StorageType.Inventory, i);

        private void OnFastSlotClicked(int i) => HandleSlotClick(StorageType.FastSlot, i);

        private void HandleSlotClick(StorageType storage, int index)
        {
            if (selectedStorage == StorageType.Null)
            {
                SetSelection(storage, index);
                return;
            }

            if (selectedStorage == storage && selectedSlotIndex == index)
            {
                ClearSelection();
                return;
            }

            swapRequestEvent?.Raise((selectedStorage, selectedSlotIndex, storage, index));
            ClearSelection();
        }

        private void OnDropClicked()
        {
            if (selectedStorage == StorageType.Null)
                return;
            dropRequestEvent?.Raise((selectedStorage, selectedSlotIndex));
            ClearSelection();
        }

        // ─── Selection — swaps CSS classes, never touches backgroundImage ────

        private void SetSelection(StorageType storage, int index)
        {
            selectedStorage = storage;
            selectedSlotIndex = index;
            SwapToSelected(Slots(storage), index, storage);
        }

        private void ClearSelection()
        {
            if (selectedStorage != StorageType.Null)
                SwapToDefault(Slots(selectedStorage), selectedSlotIndex, selectedStorage);

            selectedStorage = StorageType.Null;
            selectedSlotIndex = -1;
            ghost.OnHoverLeave();
        }

        /// Removes the default class and adds the --selected variant.
        private void SwapToSelected(List<VisualElement> slots, int index, StorageType storage)
        {
            if (index < 0 || index >= slots.Count)
                return;
            var slot = slots[index];
            if (storage == StorageType.FastSlot)
            {
                slot.AddToClassList(FastSlotSelectedClass);
            }
            else
            {
                slot.AddToClassList(InvSlotSelectedClass);
            }
        }

        /// Removes the --selected variant and restores the default class.
        private void SwapToDefault(List<VisualElement> slots, int index, StorageType storage)
        {
            if (index < 0 || index >= slots.Count)
                return;
            var slot = slots[index];
            if (storage == StorageType.FastSlot)
            {
                slot.RemoveFromClassList(FastSlotSelectedClass);
            }
            else
            {
                slot.RemoveFromClassList(InvSlotSelectedClass);
            }
        }

        // ─── Data events ─────────────────────────────────────────────────────

        private void OnLastAdded((StorageType t, string id, int pos, int qty) data)
        {
            SlotItemLoader.LoadItem(
                Icon(data.t, data.pos),
                data.id,
                cacheManager,
                worldSelector.GetSelectedWorldId(),
                data.qty
            );
            TrackSlotItem(Slots(data.t), data.pos, data.id);
        }

        private void OnLastRemoved((StorageType t, string id, int pos) data)
        {
            SlotItemLoader.LoadItem(Icon(data.t, data.pos), null, cacheManager);
            TrackSlotItem(Slots(data.t), data.pos, null);
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
            SlotItemLoader.LoadItem(
                Icon(data.srcT, data.srcI),
                data.tgtId,
                cacheManager,
                worldSelector.GetSelectedWorldId(),
                data.tgtQty
            );
            SlotItemLoader.LoadItem(
                Icon(data.tgtT, data.tgtI),
                data.srcId,
                cacheManager,
                worldSelector.GetSelectedWorldId(),
                data.srcQty
            );
            TrackSlotItem(Slots(data.srcT), data.srcI, data.tgtId);
            TrackSlotItem(Slots(data.tgtT), data.tgtI, data.srcId);

            if (!string.IsNullOrEmpty(data.srcId) || !string.IsNullOrEmpty(data.tgtId))
                soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.ChangeItem);
        }

        // ─── Utilities ───────────────────────────────────────────────────────

        private void TrackSlotItem(List<VisualElement> slots, int index, string itemId)
        {
            if (index < 0 || index >= slots.Count)
                return;
            var slot = slots[index];
            if (string.IsNullOrEmpty(itemId))
                slotItemIds.Remove(slot);
            else
                slotItemIds[slot] = itemId;
        }

        private List<VisualElement> Slots(StorageType t) =>
            t == StorageType.FastSlot ? fastSlots : inventorySlots;

        private VisualElement Icon(StorageType t, int index)
        {
            var slots = Slots(t);
            if (index < 0 || index >= slots.Count)
                return null;
            string iconName = t == StorageType.FastSlot ? "FastEquipIcon" : "ItemIcon";
            return slots[index].Q(iconName) ?? slots[index];
        }
    }
}
