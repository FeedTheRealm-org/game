using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Environment.Quest;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// Server-side quest system. Validates quest acceptance using QuestRegistry
    /// and registers the accepted quest per player (netId).
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        private ServerQuestRegistry serverQuestRegistry;

        private uint netId;

        public void Initialize(uint netId)
        {
            this.netId = netId;
        }

        public void OnQuestAccepted(IEventCollectable ec, string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                logger?.Log("[QuestSystem] OnQuestAccepted called with empty questId.", this);
                return;
            }

            if (serverQuestRegistry == null)
            {
                Debug.LogError("[QuestSystem] serverQuestRegistry is null! Injection failed.");
                return;
            }

            if (!serverQuestRegistry.IsValidQuestId(questId))
            {
                logger?.Log(
                    $"[QuestSystem] Quest '{questId}' not found in world data.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            logger?.Log($"[QuestSystem] Player {netId} accepted quest '{questId}'.", this);

            // TODO: persist quest acceptance per player
            // TODO: validate prerequisites
            // TODO: grant rewards on completion
        }
    }
}
