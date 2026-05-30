using System;
using System.Collections.Generic;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Client.Interfaces;
using FTR.Core.Client.Managers;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Registry;
using FTRShared.Runtime.Core.Cache;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.Inventory
{
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(AnimationInventoryUIController))]
    public class InventoryUIController : MonoBehaviour
    {
        private const int InventorySlotCount = 35;
        private const int FastSlotCount = 5;
        private const int InventoryColumns = 5;
        private const float SlotSize = 70f;
        private const float SlotGap = 6f;
        private const string InvSlotClass = "inventory-slot";
        private const string InvSlotSelectedClass = "inventory-slot--selected";
        private const string FastSlotClass = "fast-equip-slot";
        private const string FastSlotSelectedClass = "fast-equip-slot--selected";

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

        [Inject]
        private ConfirmPopupHandle confirmPopupHandle;

        private IConfirmPopup ConfirmPopup => confirmPopupHandle.Controller;

        [SerializeField]
        private PlayerInputReader inputReader;

        [Inject]
        private CacheManager cacheManager;

        [Header("Tooltip")]
        [SerializeField]
        private VisualTreeAsset tooltipUXML;

        [SerializeField]
        StyleSheet tooltipStyleSheet;

        private UIDocument uiDocument;
        private AnimationInventoryUIController animationController;
        private ItemStatsTooltip itemStatsTooltip;
        private VisualElement tooltipContainer;

        private readonly List<VisualElement> inventorySlots = new(InventorySlotCount);
        private readonly List<VisualElement> fastSlots = new(FastSlotCount);
        private readonly Dictionary<VisualElement, string> slotItemIds = new();

        private int selectedSlotIndex = -1;
        private StorageType selectedStorage;

        private readonly InventorySlotGhostController ghost = new();

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            animationController = GetComponent<AnimationInventoryUIController>();

            itemStatsTooltip = GetComponent<ItemStatsTooltip>();

            var root = uiDocument.rootVisualElement;
            if (root == null)
                return;

            if (tooltipUXML != null)
            {
                var tooltipTree = tooltipUXML.Instantiate();
                tooltipContainer = tooltipTree.Q("TooltipContainer") ?? tooltipTree;
                tooltipContainer.style.position = Position.Absolute;
                tooltipContainer.style.display = DisplayStyle.None;
                tooltipContainer.style.left = 0;
                tooltipContainer.style.top = 0;

                if (tooltipStyleSheet != null && !root.styleSheets.Contains(tooltipStyleSheet))
                    root.styleSheets.Add(tooltipStyleSheet);

                root.Add(tooltipContainer);

                if (itemStatsTooltip != null)
                {
                    itemStatsTooltip.Initialize(tooltipContainer);
                }
                else
                {
                    Debug.LogWarning("ItemStatsTooltip component is missing on this GameObject!");
                }
            }

            animationController.Initialize(root);

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

            if (tooltipContainer != null && tooltipContainer.parent != null)
                tooltipContainer.RemoveFromHierarchy();
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
            ConfirmPopup?.Show(
                title: "Drop Item",
                question: "Are you sure you want to drop this item?",
                onConfirm: Drop,
                onCancel: null
            );
        }

        private void Drop()
        {
            dropRequestEvent?.Raise((selectedStorage, selectedSlotIndex));
            ClearSelection();
        }

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

        private void OnLastAdded((StorageType t, string id, int pos, int qty) data)
        {
            SlotItemLoader.LoadItem(
                Icon(data.t, data.pos),
                data.id,
                cacheManager,
                worldSelector,
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
                worldSelector,
                data.tgtQty
            );
            SlotItemLoader.LoadItem(
                Icon(data.tgtT, data.tgtI),
                data.srcId,
                cacheManager,
                worldSelector,
                data.srcQty
            );
            TrackSlotItem(Slots(data.srcT), data.srcI, data.tgtId);
            TrackSlotItem(Slots(data.tgtT), data.tgtI, data.srcId);

            if (!string.IsNullOrEmpty(data.srcId) || !string.IsNullOrEmpty(data.tgtId))
                soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.ChangeItem);
        }

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
