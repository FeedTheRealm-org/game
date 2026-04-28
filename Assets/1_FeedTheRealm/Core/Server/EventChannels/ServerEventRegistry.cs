using UnityEngine;
using VContainer;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(fileName = "ServerEventRegistry", menuName = "Events/ServerEventRegistry")]
    public class ServerEventRegistry : ScriptableObject
    {
        [Header("Tick Events")]
        public GameTickEvent gameTickEvent;

        [Header("Quest Events")]
        public EnemySlayedEvent enemySlayedEvent;
        public NpcInteractedEvent npcInteractedEvent;
        public QuestRewardGoldEvent questRewardGoldEvent;
        public QuestRewardItemEvent questRewardItemEvent;
        public NpcQuestCompletedEvent npcQuestCompletedEvent;
        public PlayerQuestDecisionEvent playerQuestDecisionEvent;

        public void RegisterAll(IContainerBuilder builder)
        {
            Validate();

            builder.RegisterInstance(gameTickEvent);
            builder.RegisterInstance(enemySlayedEvent);
            builder.RegisterInstance(npcInteractedEvent);
            builder.RegisterInstance(questRewardGoldEvent);
            builder.RegisterInstance(questRewardItemEvent);
            builder.RegisterInstance(npcQuestCompletedEvent);
            builder.RegisterInstance(playerQuestDecisionEvent);
        }

        private void Validate()
        {
            ValidateField(gameTickEvent, nameof(gameTickEvent));
            ValidateField(enemySlayedEvent, nameof(enemySlayedEvent));
            ValidateField(npcInteractedEvent, nameof(npcInteractedEvent));
            ValidateField(questRewardGoldEvent, nameof(questRewardGoldEvent));
            ValidateField(questRewardItemEvent, nameof(questRewardItemEvent));
            ValidateField(npcQuestCompletedEvent, nameof(npcQuestCompletedEvent));
            ValidateField(playerQuestDecisionEvent, nameof(playerQuestDecisionEvent));
        }

        private void ValidateField(Object field, string fieldName)
        {
            if (field == null)
                throw new System.Exception($"[ServerEventRegistry] {fieldName} is not assigned.");
        }
    }
}
