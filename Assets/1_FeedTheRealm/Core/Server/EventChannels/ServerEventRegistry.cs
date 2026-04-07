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

        public void RegisterAll(IContainerBuilder builder)
        {
            Validate();

            builder.RegisterInstance(gameTickEvent);
            builder.RegisterInstance(enemySlayedEvent);
            builder.RegisterInstance(npcInteractedEvent);
        }

        private void Validate()
        {
            ValidateField(gameTickEvent, nameof(gameTickEvent));
            ValidateField(enemySlayedEvent, nameof(enemySlayedEvent));
            ValidateField(npcInteractedEvent, nameof(npcInteractedEvent));
        }

        private void ValidateField(Object field, string fieldName)
        {
            if (field == null)
                throw new System.Exception($"[ClientEventRegistry] {fieldName} is not assigned.");
        }
    }
}
