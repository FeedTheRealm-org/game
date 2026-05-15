using FTR.Core.Client.EventChannels.Quest;
using FTR.Core.Common.Enums;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Environment.Quest;
using FTR.Gameplay.Client.Registry;
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
        private ShowQuestPromptEvent showQuestPromptEvent;

        [Inject]
        private QuestDecisionEvent questDecisionEvent;

        [Inject]
        private ClientQuestRegistry clientQuestRegistry;

        [Inject]
        private ISoundPlayer soundPlayer;

        private NetworkAdapter networkAdapter;
        private bool isInitialized;

        public void Initialize(NetworkAdapter networkAdapter)
        {
            this.networkAdapter = networkAdapter;
            isInitialized = true;

            npcQuestOfferedEvent.OnRaised += HandleQuestOffered;
            questDecisionEvent.OnRaised += HandleQuestDecision;
        }

        private void OnDestroy()
        {
            if (npcQuestOfferedEvent != null)
                npcQuestOfferedEvent.OnRaised -= HandleQuestOffered;
            if (questDecisionEvent != null)
                questDecisionEvent.OnRaised -= HandleQuestDecision;
        }

        private void HandleQuestOffered((string questId, string npcId) data)
        {
            if (!isInitialized || string.IsNullOrEmpty(data.questId))
            {
                Debug.LogWarning(
                    $"[QuestView] Aborting HandleQuestOffered. initialized: {isInitialized}"
                );
                return;
            }

            if (!clientQuestRegistry.TryGetQuest(data.questId, out var questData))
            {
                Debug.LogWarning(
                    $"[QuestView] Quest '{data.questId}' not found in ClientQuestRegistry."
                );
                return;
            }

            showQuestPromptEvent.Raise(
                new QuestPromptData(questData, networkAdapter.netId, data.npcId)
            );
        }

        private void HandleQuestDecision(QuestDecisionData decision)
        {
            if (decision.TargetNetId != networkAdapter.netId)
            {
                return;
            }

            if (decision.IsAccepted)
            {
                networkAdapter.DispatchTransaction(
                    new TransactionCommandDTO
                    {
                        Type = TransactionType.AcceptQuest,
                        Id = decision.Quest.id,
                    }
                );
                soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.QuestAccept);
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
