using FTR.Core.Client.EventChannels.Interaction;
using FTR.Core.Client.StateMachine;
using FTR.Core.Common.EventChannels;
using FTR.Gameplay.Client.Characters.Shared.StateMachine;
using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Client.Characters.Shared.StateMachine.States
{
    /// <summary>
    /// Active while the player is interacting with any server-side IInteractable.
    /// While active, pressing Interact dispatches ContinueInteraction (DialogNext) to the server
    /// rather than starting a new interaction, so the server drives all progression logic.
    /// </summary>
    public class CharacterInteractingState : IActionState
    {
        private IStateMachine stateMachine;
        private InteractController interactController;
        private NpcDialogClosedEvent npcDialogClosedEvent;
        private ShowQuestPromptEvent showQuestPromptEvent;
        private InteractFailedEvent interactFailedEvent;
        private InteractCompletedEvent interactCompletedEvent;
        private CharacterAnimator animator;

        public CharacterInteractingState(
            IStateMachine stateMachine,
            InteractController interactController,
            NpcDialogClosedEvent npcDialogClosedEvent,
            ShowQuestPromptEvent showQuestPromptEvent,
            InteractFailedEvent interactFailedEvent,
            InteractCompletedEvent interactCompletedEvent,
            CharacterAnimator animator
        )
        {
            this.stateMachine = stateMachine;
            this.interactController = interactController;
            this.npcDialogClosedEvent = npcDialogClosedEvent;
            this.showQuestPromptEvent = showQuestPromptEvent;
            this.interactFailedEvent = interactFailedEvent;
            this.interactCompletedEvent = interactCompletedEvent;
            this.animator = animator;
        }

        public void Enter()
        {
            npcDialogClosedEvent.OnRaised += OnDialogClosed;
            interactFailedEvent.OnRaised += OnInteractFailed;
            interactCompletedEvent.OnRaised += OnInteractCompleted;

            if (showQuestPromptEvent != null)
                showQuestPromptEvent.OnRaised += OnShowQuestPrompt;
        }

        public void Exit()
        {
            npcDialogClosedEvent.OnRaised -= OnDialogClosed;
            interactFailedEvent.OnRaised -= OnInteractFailed;
            interactCompletedEvent.OnRaised -= OnInteractCompleted;

            if (showQuestPromptEvent != null)
                showQuestPromptEvent.OnRaised -= OnShowQuestPrompt;
        }

        public void OnInteractWhileActive()
        {
            interactController.OnDialogNext();
        }

        private void OnInteractFailed()
        {
            stateMachine.SetActionState(null);
        }

        private void OnInteractCompleted()
        {
            stateMachine.SetActionState(null);
        }

        private void OnDialogClosed()
        {
            if (stateMachine is CharacterStateMachine csm)
                csm.OnDialogClosed();
            stateMachine.SetActionState(null);
        }

        private void OnShowQuestPrompt(QuestPromptData data)
        {
            var questState = stateMachine.GetActionStateByType(typeof(CharacterQuestState));
            if (questState != null)
                stateMachine.SetActionState(questState);
        }

        public void Dispose()
        {
            stateMachine = null;
            interactController = null;
            npcDialogClosedEvent = null;
            showQuestPromptEvent = null;
            interactFailedEvent = null;
            interactCompletedEvent = null;
            animator = null;
        }
    }
}
