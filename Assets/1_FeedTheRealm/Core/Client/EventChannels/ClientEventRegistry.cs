using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Client.EventChannels.Shop;
using FTR.Core.Client.EventChannels.Status;
using FTR.Core.Client.EventChannels.Ticks;
using UnityEngine;
using VContainer;

namespace FeedTheRealm.Core.Client.EventChannels
{
    [CreateAssetMenu(fileName = "ClientEventRegistry", menuName = "Events/ClientEventRegistry")]
    public class ClientEventRegistry : ScriptableObject
    {
        [Header("Shop Events")]
        public ShopInteractedEvent shopInteractedEvent;
        public ShopOnCloseEvent shopOnCloseEvent;

        [Header("Status Events")]
        public HealthChangedEvent healthChangedEvent;
        public StaminaChangedEvent staminaChangedEvent;

        [Header("Inventory Events")]
        public LastAddedEvent lastAddedEvent;
        public LastSwappedEvent lastSwappedEvent;
        public LastRemovedEvent lastRemovedEvent;
        public SlotEquipRequestEvent slotEquipRequestEvent;
        public SlotUnequipRequestEvent slotUnequipRequestEvent;
        public SlotSwapRequestEvent slotSwapRequestEvent;
        public SlotDropRequestEvent slotDropRequestEvent;
        public InventoryToggleEvent inventoryToggleEvent;

        [Header("Tick Events")]
        public TickEvent tickEvent;
        public FixedTickEvent fixedTickEvent;
        public LateTickEvent lateTickEvent;

        public void RegisterAll(IContainerBuilder builder)
        {
            Validate();

            builder.RegisterInstance(shopInteractedEvent);
            builder.RegisterInstance(shopOnCloseEvent);
            builder.RegisterInstance(healthChangedEvent);
            builder.RegisterInstance(staminaChangedEvent);
            builder.RegisterInstance(lastAddedEvent);
            builder.RegisterInstance(lastSwappedEvent);
            builder.RegisterInstance(lastRemovedEvent);
            builder.RegisterInstance(slotEquipRequestEvent);
            builder.RegisterInstance(slotUnequipRequestEvent);
            builder.RegisterInstance(slotSwapRequestEvent);
            builder.RegisterInstance(slotDropRequestEvent);
            builder.RegisterInstance(inventoryToggleEvent);
            builder.RegisterInstance(tickEvent);
            builder.RegisterInstance(fixedTickEvent);
            builder.RegisterInstance(lateTickEvent);
        }

        private void Validate()
        {
            ValidateField(shopInteractedEvent, nameof(shopInteractedEvent));
            ValidateField(shopOnCloseEvent, nameof(shopOnCloseEvent));
            ValidateField(healthChangedEvent, nameof(healthChangedEvent));
            ValidateField(staminaChangedEvent, nameof(staminaChangedEvent));
            ValidateField(lastAddedEvent, nameof(lastAddedEvent));
            ValidateField(lastSwappedEvent, nameof(lastSwappedEvent));
            ValidateField(lastRemovedEvent, nameof(lastRemovedEvent));
            ValidateField(slotEquipRequestEvent, nameof(slotEquipRequestEvent));
            ValidateField(slotUnequipRequestEvent, nameof(slotUnequipRequestEvent));
            ValidateField(slotSwapRequestEvent, nameof(slotSwapRequestEvent));
            ValidateField(slotDropRequestEvent, nameof(slotDropRequestEvent));
            ValidateField(inventoryToggleEvent, nameof(inventoryToggleEvent));
            ValidateField(tickEvent, nameof(tickEvent));
            ValidateField(fixedTickEvent, nameof(fixedTickEvent));
            ValidateField(lateTickEvent, nameof(lateTickEvent));
        }

        private void ValidateField(Object field, string fieldName)
        {
            if (field == null)
                Debug.LogError($"[ClientEventRegistry] {fieldName} is not assigned.");
        }
    }
}
