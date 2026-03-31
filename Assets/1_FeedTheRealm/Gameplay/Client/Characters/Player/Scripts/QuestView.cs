using FTR.Core.Client.EventChannels.Quest;
using FTR.Core.Common.Enums;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Environment.Quest;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Characters.Player
{
    /// <summary>
    /// Client-side view that reacts to quest offers arriving via NpcQuestOfferedEvent.
    /// </summary>
    public class QuestView : MonoBehaviour
    {
        [Inject]
        private NpcQuestOfferedEvent npcQuestOfferedEvent;

        [Inject]
        private QuestOfferedEvent questOfferedEvent;

        [Inject]
        private QuestDecisionEvent questDecisionEvent;

        [Inject]
        private ClientQuestRegistry clientQuestRegistry;

        private NetworkAdapter networkAdapter;
        private bool isInitialized;

        public void Initialize(NetworkAdapter networkAdapter)
        {
            this.networkAdapter = networkAdapter;
            isInitialized = true;

            npcQuestOfferedEvent.OnRaised += HandleQuestOffered;
            questDecisionEvent.OnRaised += HandleQuestDecision;
            Debug.Log($"[QuestView] Initialized and subscribed to events.");
        }

        private void OnDestroy()
        {
            if (npcQuestOfferedEvent != null)
                npcQuestOfferedEvent.OnRaised -= HandleQuestOffered;
            if (questDecisionEvent != null)
                questDecisionEvent.OnRaised -= HandleQuestDecision;
        }

        private void HandleQuestOffered(string questId)
        {
            Debug.Log($"[QuestView] HandleQuestOffered triggered for questId: {questId}");
            if (!isInitialized || string.IsNullOrEmpty(questId))
            {
                Debug.LogWarning(
                    $"[QuestView] Aborting HandleQuestOffered. initialized: {isInitialized}"
                );
                return;
            }

            if (!clientQuestRegistry.TryGetQuest(questId, out var questData))
            {
                Debug.LogWarning(
                    $"[QuestView] Quest '{questId}' not found in ClientQuestRegistry."
                );
                return;
            }

            Debug.Log(
                $"[QuestView] Raising questOfferedEvent for quest: {questData.id} - {questData.title}"
            );

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            questOfferedEvent.Raise(questData);
        }

        private void HandleQuestDecision(QuestDecisionData decision)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (decision.IsAccepted)
            {
                networkAdapter.DispatchTransaction(
                    new TransactionCommandDTO
                    {
                        Type = TransactionType.AcceptQuest,
                        Id = decision.Quest.id,
                    }
                );
            }
            else
            {
                networkAdapter.DispatchTransaction(
                    new TransactionCommandDTO
                    {
                        Type = TransactionType.RejectQuest,
                        Id = string.Empty,
                    }
                );
            }

            networkAdapter.DispatchAction(new ActionCommandDTO { Type = ActionType.DialogNext });
        }
    }
}
