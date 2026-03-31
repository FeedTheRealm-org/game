using FTR.Core.Client.StateMachine;
using FTR.Core.Common.EventChannels;
using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Client.Characters.Shared.StateMachine.States
{
    /// <summary>
    /// Active while a quest prompt is shown to the player.
    /// Interact input is fully blocked — the player must accept or reject via UI buttons.
    /// Exits back to CharacterInteractingState when QuestDecisionEvent fires (accept or reject),
    /// or exits to null if the dialog closes server-side while the prompt is open.
    /// </summary>
    public class CharacterQuestState : IActionState
    {
        private IStateMachine stateMachine;
        private QuestDecisionEvent questDecisionEvent;
        private NpcDialogClosedEvent npcDialogClosedEvent;

        public CharacterQuestState(
            IStateMachine stateMachine,
            QuestDecisionEvent questDecisionEvent,
            NpcDialogClosedEvent npcDialogClosedEvent
        )
        {
            this.stateMachine = stateMachine;
            this.questDecisionEvent = questDecisionEvent;
            this.npcDialogClosedEvent = npcDialogClosedEvent;
        }

        public void Enter()
        {
            questDecisionEvent.OnRaised += OnQuestDecided;
            npcDialogClosedEvent.OnRaised += OnDialogClosed;
        }

        public void Exit()
        {
            questDecisionEvent.OnRaised -= OnQuestDecided;
            npcDialogClosedEvent.OnRaised -= OnDialogClosed;
        }

        private void OnQuestDecided(QuestDecisionData _)
        {
            var interactingState = stateMachine.GetActionStateByType(
                typeof(CharacterInteractingState)
            );
            stateMachine.SetActionState(interactingState);
        }

        private void OnDialogClosed()
        {
            if (stateMachine is CharacterStateMachine csm)
                csm.OnDialogClosed();

            stateMachine.SetActionState(null);
        }

        public void Dispose()
        {
            stateMachine = null;
            questDecisionEvent = null;
            npcDialogClosedEvent = null;
        }
    }
}
