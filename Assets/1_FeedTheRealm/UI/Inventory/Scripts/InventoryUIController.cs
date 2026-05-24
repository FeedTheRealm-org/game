using System;
using System.Collections.Generic;
using FTR.Core.Client.EventChannels.Input;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Client.Managers;
using FTR.Core.Common.Protocol.RpcMessages;
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
        private const int InventorySlotCount = 12;
        private const int FastSlotCount = 5;

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
        private ISoundPlayer soundPlayer;

        [Inject]
        private MenuManager menuManager;

        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private Sprite defaultSlotSprite;

        [SerializeField]
        private Sprite selectedSlotSprite;

        [SerializeField]
        private API.ItemAssetsService itemAssetsService;

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
            if (show)
                soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.OpenUI);
            else
                soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.CloseUI);

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
            dropRequestEvent?.Raise((selectedStorage, selectedSlotIndex));
            ClearSelection();
        }

        private void SetSelection(StorageType storage, int index)
        {
            selectedStorage = storage;
            selectedSlotIndex = index;
            SetSlotSprite(Slots(storage), index, selectedSlotSprite);
        }

        private void ClearSelection()
        {
            if (selectedStorage != StorageType.Null)
                SetSlotSprite(Slots(selectedStorage), selectedSlotIndex, defaultSlotSprite);

            selectedStorage = StorageType.Null;
            selectedSlotIndex = -1;
            ghost.OnHoverLeave();
        }

        private void SetSlotSprite(List<VisualElement> slots, int index, Sprite sprite)
        {
            if (sprite != null && index >= 0 && index < slots.Count)
                slots[index].style.backgroundImage = new StyleBackground(sprite);
        }

        private void OnLastAdded((StorageType t, string id, int pos, int qty) data)
        {
            var icon = Icon(data.t, data.pos);
            SlotItemLoader.LoadItem(icon, data.id, itemAssetsService, data.qty);
            TrackSlotItem(Slots(data.t), data.pos, data.id);
        }

        private void OnLastRemoved((StorageType t, string id, int pos) data)
        {
            var icon = Icon(data.t, data.pos);
            SlotItemLoader.LoadItem(icon, null, itemAssetsService);
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
                itemAssetsService,
                data.tgtQty
            );
            SlotItemLoader.LoadItem(
                Icon(data.tgtT, data.tgtI),
                data.srcId,
                itemAssetsService,
                data.srcQty
            );
            TrackSlotItem(Slots(data.srcT), data.srcI, data.tgtId);
            TrackSlotItem(Slots(data.tgtT), data.tgtI, data.srcId);

            if (!string.IsNullOrEmpty(data.srcId) || !string.IsNullOrEmpty(data.tgtId))
            {
                soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.ChangeItem);
            }
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
