using FeedTheRealm.Core.EventChannels.Setup;
using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Chat;
using FTR.Core.Client.EventChannels.Gold;
using FTR.Core.Client.EventChannels.Input;
using FTR.Core.Client.EventChannels.Interaction;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Client.EventChannels.Portal;
using FTR.Core.Client.EventChannels.Quest;
using FTR.Core.Client.EventChannels.Shop;
using FTR.Core.Client.EventChannels.Status;
using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Client.EventChannels.UI;
using FTR.Core.Common.EventChannels;
using UnityEngine;
using VContainer;

namespace FeedTheRealm.Core.Client.EventChannels
{
    [CreateAssetMenu(fileName = "ClientEventRegistry", menuName = "Events/ClientEventRegistry")]
    public class ClientEventRegistry : ScriptableObject
    {
        [Header("Setup Events")]
        public LoadingEvent loadingEvent;
        public LoadingProgressEvent loadingProgressEvent;

        [Header("Shop Events")]
        public ShopToggleEvent shopToggleEvent;
        public ShopInteractedEvent shopInteractedEvent;
        public OpenShopEvent openShopEvent;
        public PurchaseRequestEvent purchaseRequestEvent;

        [Header("Status Events")]
        public HealthChangedEvent healthChangedEvent;
        public StaminaChangedEvent staminaChangedEvent;

        [Header("Inventory Events")]
        public LastAddedEvent lastAddedEvent;
        public LastSwappedEvent lastSwappedEvent;
        public LastRemovedEvent lastRemovedEvent;
        public ActiveSlotChangedEvent ActiveSlotChangedEvent;
        public SlotEquipRequestEvent slotEquipRequestEvent;
        public SlotSwapRequestEvent slotSwapRequestEvent;
        public SlotDropRequestEvent slotDropRequestEvent;
        public InventoryToggleEvent inventoryToggleEvent;
        public InventoryErrorEvent inventoryErrorEvent;

        [Header("Interact Events")]
        public InteractFailedEvent interactFailedEvent;
        public InteractCompletedEvent interactCompletedEvent;

        [Header("NPC Events")]
        public NpcInteractedEvent npcInteractedEvent;
        public NpcDialogClosedEvent npcDialogClosedEvent;

        [Header("Quest Events")]
        public NpcQuestOfferedEvent npcQuestOfferedEvent;
        public ShowQuestPromptEvent showQuestPromptEvent;
        public QuestDecisionEvent questDecisionEvent;
        public QuestCompletedEvent questCompletedEvent;
        public QuestTrackToggleEvent questTrackToggleEvent;

        [Header("Tick Events")]
        public TickEvent tickEvent;
        public FixedTickEvent fixedTickEvent;
        public LateTickEvent lateTickEvent;

        [Header("Gold Events")]
        public GoldChangedEvent goldChangedEvent;
        public GemBalanceChangedEvent gemBalanceChangedEvent;

        [Header("Chat Events")]
        public ChatMessageRequestEvent chatMessageRequestEvent;
        public ChatToggleEvent chatToggleEvent;

        [Header("Portal Events")]
        public PortalToggleEvent portalToggleEvent;
        public OpenPortalUIEvent openPortalUIEvent;

        [Header("UI Related Events")]
        public OnWorldLeaveEvent onExitEvent;
        public OnProfileCreatedEvent onProfileCreatedEvent;
        public OnLogoutRequestedEvent onLogoutRequestedEvent;
        public QuestMenuToggleEvent questToggleEvent;

        [Header("Input Related Events")]
        public BackEvent backEvent;

        public void RegisterAll(IContainerBuilder builder)
        {
            Validate();

            builder.RegisterInstance(shopToggleEvent);
            builder.RegisterInstance(shopInteractedEvent);
            builder.RegisterInstance(healthChangedEvent);
            builder.RegisterInstance(staminaChangedEvent);
            builder.RegisterInstance(lastAddedEvent);
            builder.RegisterInstance(lastSwappedEvent);
            builder.RegisterInstance(lastRemovedEvent);
            builder.RegisterInstance(ActiveSlotChangedEvent);
            builder.RegisterInstance(slotEquipRequestEvent);
            builder.RegisterInstance(slotSwapRequestEvent);
            builder.RegisterInstance(slotDropRequestEvent);
            builder.RegisterInstance(inventoryToggleEvent);
            builder.RegisterInstance(tickEvent);
            builder.RegisterInstance(fixedTickEvent);
            builder.RegisterInstance(lateTickEvent);
            builder.RegisterInstance(npcInteractedEvent);
            builder.RegisterInstance(npcDialogClosedEvent);
            builder.RegisterInstance(npcQuestOfferedEvent);
            builder.RegisterInstance(showQuestPromptEvent);
            builder.RegisterInstance(questDecisionEvent);
            builder.RegisterInstance(questCompletedEvent);
            builder.RegisterInstance(questTrackToggleEvent);
            builder.RegisterInstance(goldChangedEvent);
            builder.RegisterInstance(gemBalanceChangedEvent);
            builder.RegisterInstance(openShopEvent);
            builder.RegisterInstance(purchaseRequestEvent);
            builder.RegisterInstance(interactFailedEvent);
            builder.RegisterInstance(interactCompletedEvent);
            builder.RegisterInstance(inventoryErrorEvent);
            builder.RegisterInstance(chatMessageRequestEvent);
            builder.RegisterInstance(chatToggleEvent);
            builder.RegisterInstance(portalToggleEvent);
            builder.RegisterInstance(openPortalUIEvent);
            builder.RegisterInstance(loadingEvent);
            builder.RegisterInstance(loadingProgressEvent);
            builder.RegisterInstance(onExitEvent);
            builder.RegisterInstance(onProfileCreatedEvent);
            builder.RegisterInstance(backEvent);
            builder.RegisterInstance(onLogoutRequestedEvent);
            builder.RegisterInstance(questToggleEvent);
        }

        private void Validate()
        {
            ValidateField(shopToggleEvent, nameof(shopToggleEvent));
            ValidateField(shopInteractedEvent, nameof(shopInteractedEvent));
            ValidateField(healthChangedEvent, nameof(healthChangedEvent));
            ValidateField(staminaChangedEvent, nameof(staminaChangedEvent));
            ValidateField(lastAddedEvent, nameof(lastAddedEvent));
            ValidateField(lastSwappedEvent, nameof(lastSwappedEvent));
            ValidateField(lastRemovedEvent, nameof(lastRemovedEvent));
            ValidateField(ActiveSlotChangedEvent, nameof(ActiveSlotChangedEvent));
            ValidateField(slotEquipRequestEvent, nameof(slotEquipRequestEvent));
            ValidateField(slotSwapRequestEvent, nameof(slotSwapRequestEvent));
            ValidateField(slotDropRequestEvent, nameof(slotDropRequestEvent));
            ValidateField(inventoryToggleEvent, nameof(inventoryToggleEvent));
            ValidateField(tickEvent, nameof(tickEvent));
            ValidateField(fixedTickEvent, nameof(fixedTickEvent));
            ValidateField(lateTickEvent, nameof(lateTickEvent));
            ValidateField(npcDialogClosedEvent, nameof(npcDialogClosedEvent));
            ValidateField(npcQuestOfferedEvent, nameof(npcQuestOfferedEvent));
            ValidateField(showQuestPromptEvent, nameof(showQuestPromptEvent));
            ValidateField(questDecisionEvent, nameof(questDecisionEvent));
            ValidateField(questCompletedEvent, nameof(questCompletedEvent));
            ValidateField(goldChangedEvent, nameof(goldChangedEvent));
            ValidateField(gemBalanceChangedEvent, nameof(gemBalanceChangedEvent));
            ValidateField(openShopEvent, nameof(openShopEvent));
            ValidateField(purchaseRequestEvent, nameof(purchaseRequestEvent));
            ValidateField(npcInteractedEvent, nameof(npcInteractedEvent));
            ValidateField(interactFailedEvent, nameof(interactFailedEvent));
            ValidateField(interactCompletedEvent, nameof(interactCompletedEvent));
            ValidateField(inventoryErrorEvent, nameof(inventoryErrorEvent));
            ValidateField(chatMessageRequestEvent, nameof(chatMessageRequestEvent));
            ValidateField(chatToggleEvent, nameof(chatToggleEvent));
            ValidateField(portalToggleEvent, nameof(portalToggleEvent));
            ValidateField(openPortalUIEvent, nameof(openPortalUIEvent));
            ValidateField(loadingEvent, nameof(loadingEvent));
            ValidateField(loadingProgressEvent, nameof(loadingProgressEvent));
            ValidateField(onExitEvent, nameof(onExitEvent));
            ValidateField(onProfileCreatedEvent, nameof(onProfileCreatedEvent));
            ValidateField(backEvent, nameof(backEvent));
            ValidateField(onLogoutRequestedEvent, nameof(onLogoutRequestedEvent));
            ValidateField(questToggleEvent, nameof(questToggleEvent));
        }

        private void ValidateField(Object field, string fieldName)
        {
            if (field == null)
                throw new System.Exception($"[ClientEventRegistry] {fieldName} is not assigned.");
        }
    }
}
