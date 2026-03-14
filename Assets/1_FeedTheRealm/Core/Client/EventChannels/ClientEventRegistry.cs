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
        public LastItemChangedEvent lastItemChangedEvent;
        public LastSwappedItemChangedEvent lastSwappedItemChangedEvent;
        public LastDroppedItemChangedEvent lastDroppedItemChangedEvent;
        public InventorySlotSwapRequestEvent inventorySlotSwapRequestEvent;
        public InventorySlotDropRequestEvent inventorySlotDropRequestEvent;
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
            builder.RegisterInstance(lastItemChangedEvent);
            builder.RegisterInstance(lastSwappedItemChangedEvent);
            builder.RegisterInstance(lastDroppedItemChangedEvent);
            builder.RegisterInstance(inventorySlotSwapRequestEvent);
            builder.RegisterInstance(inventorySlotDropRequestEvent);
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
            ValidateField(lastItemChangedEvent, nameof(lastItemChangedEvent));
            ValidateField(lastSwappedItemChangedEvent, nameof(lastSwappedItemChangedEvent));
            ValidateField(lastDroppedItemChangedEvent, nameof(lastDroppedItemChangedEvent));
            ValidateField(inventorySlotSwapRequestEvent, nameof(inventorySlotSwapRequestEvent));
            ValidateField(inventorySlotDropRequestEvent, nameof(inventorySlotDropRequestEvent));
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
