using FTR.Core.Client.StateMachine;
using FTR.Core.Common.EventChannels;
using FTR.Gameplay.Client.Characters.Shared.StateMachine;
using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Client.Characters.Shared.StateMachine.States
{
    /// <summary>
    /// State for when the character is interacting with an NPC.
    /// Pressing Interact again dispatches DialogNext to the server, which either
    /// advances to the next message or closes the dialog.
    /// When a QuestOfferedEvent arrives (raised by QuestView after resolving QuestData),
    /// transitions to CharacterQuestState so interact input is blocked during the prompt.
    /// </summary>
    public class CharacterInteractingState : IActionState
    {
        private IStateMachine stateMachine;
        private InteractController interactController;
        private NpcDialogClosedEvent npcDialogClosedEvent;
        private QuestOfferedEvent questOfferedEvent;
        private CharacterAnimator animator;

        public CharacterInteractingState(
            IStateMachine stateMachine,
            InteractController interactController,
            NpcDialogClosedEvent npcDialogClosedEvent,
            QuestOfferedEvent questOfferedEvent,
            CharacterAnimator animator
        )
        {
            this.stateMachine = stateMachine;
            this.interactController = interactController;
            this.npcDialogClosedEvent = npcDialogClosedEvent;
            this.questOfferedEvent = questOfferedEvent;
            this.animator = animator;
        }

        public void Enter()
        {
            npcDialogClosedEvent.OnRaised += OnDialogClosed;
            if (questOfferedEvent != null)
                questOfferedEvent.OnRaised += OnQuestOffered;
        }

        public void Exit()
        {
            npcDialogClosedEvent.OnRaised -= OnDialogClosed;
            if (questOfferedEvent != null)
                questOfferedEvent.OnRaised -= OnQuestOffered;
        }

        /// <summary>
        /// Dispatches DialogNext so the server advances or closes the dialog.
        /// </summary>
        public void OnInteractWhileActive()
        {
            interactController.OnDialogNext();
        }

        private void OnQuestOffered(QuestData _)
        {
            var questState = stateMachine.GetActionStateByType(typeof(CharacterQuestState));
            if (questState != null)
                stateMachine.SetActionState(questState);
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
            interactController = null;
            npcDialogClosedEvent = null;
            questOfferedEvent = null;
            animator = null;
        }
    }
}
