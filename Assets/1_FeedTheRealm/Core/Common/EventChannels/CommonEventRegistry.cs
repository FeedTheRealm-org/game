using UnityEngine;
using VContainer;

namespace FTR.Core.Common.EventChannels
{
    /// <summary>
    /// A single ScriptableObject that holds references to all event channels in the project.
    /// Assign all event SO assets here once, then pass this registry to the DI container
    /// via RegisterAll() instead of registering each event individually.
    /// </summary>
    [CreateAssetMenu(fileName = "CommonEventRegistry", menuName = "Events/CommonEventRegistry")]
    public class CommonEventRegistry : ScriptableObject
    {
        [Header("Player Events")]
        public InitiatePlayerEvent initiatePlayerEvent;
        public NpcInteractedEvent npcInteractedEvent;
        public NpcDialogClosedEvent npcDialogClosedEvent;
        public NpcDialogMessageEvent npcDialogMessageEvent;
        public NpcDialogToggledEvent npcDialogToggledEvent;
        public EnemySlayedEvent enemySlayedEvent;

        [Header("Quest Events")]
        public ShowQuestPromptEvent showQuestPromptEvent;
        public QuestProgressEvent questProgressEvent;
        public QuestDecisionEvent questDecisionEvent;
        public QuestCompletedEvent questCompletedEvent;

        [Header("Network Events")]
        public ReceivedActionCommandEvent receivedActionCommandEvent;
        public ReceivedTransactionCommandEvent receivedTransactionCommandEvent;

        /// <summary>
        /// Registers all event channels as singleton instances in the VContainer builder.
        /// Call this once from LifetimeScope.Configure() instead of registering each event manually.
        /// </summary>
        public void RegisterAll(IContainerBuilder builder)
        {
            Validate();

            builder.RegisterInstance(initiatePlayerEvent);
            builder.RegisterInstance(npcInteractedEvent);
            builder.RegisterInstance(npcDialogClosedEvent);
            builder.RegisterInstance(npcDialogMessageEvent);
            builder.RegisterInstance(npcDialogToggledEvent);
            builder.RegisterInstance(enemySlayedEvent);
            builder.RegisterInstance(showQuestPromptEvent);
            builder.RegisterInstance(questProgressEvent);
            builder.RegisterInstance(questDecisionEvent);
            builder.RegisterInstance(questCompletedEvent);
            builder.RegisterInstance(receivedActionCommandEvent);
            builder.RegisterInstance(receivedTransactionCommandEvent);
        }

        private void Validate()
        {
            ValidateField(initiatePlayerEvent, nameof(initiatePlayerEvent));
            ValidateField(npcInteractedEvent, nameof(npcInteractedEvent));
            ValidateField(npcDialogClosedEvent, nameof(npcDialogClosedEvent));
            ValidateField(npcDialogMessageEvent, nameof(npcDialogMessageEvent));
            ValidateField(npcDialogToggledEvent, nameof(npcDialogToggledEvent));
            ValidateField(enemySlayedEvent, nameof(enemySlayedEvent));
            ValidateField(showQuestPromptEvent, nameof(showQuestPromptEvent));
            ValidateField(questProgressEvent, nameof(questProgressEvent));
            ValidateField(questDecisionEvent, nameof(questDecisionEvent));
            ValidateField(questCompletedEvent, nameof(questCompletedEvent));
            ValidateField(receivedActionCommandEvent, nameof(receivedActionCommandEvent));
            ValidateField(receivedTransactionCommandEvent, nameof(receivedTransactionCommandEvent));
        }

        private void ValidateField(Object field, string fieldName)
        {
            if (field == null)
                throw new System.Exception($"[CommonEventRegistry] {fieldName} is not assigned.");
        }
    }
}
