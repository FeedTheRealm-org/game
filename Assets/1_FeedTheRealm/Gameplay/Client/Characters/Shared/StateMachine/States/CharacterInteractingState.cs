using FTR.Core.Client.StateMachine;
using FTR.Core.Common.EventChannels;

namespace FTR.Gameplay.Client.Characters.Shared.StateMachine.States
{
    /// <summary>
    /// State for when the character is interacting with an NPC.
    /// </summary>
    public class CharacterInteractingState : IActionState
    {
        private IStateMachine stateMachine;
        private InteractController interactController;
        private NpcDialogClosedEvent npcDialogClosedEvent;
        private CharacterAnimator animator;

        public CharacterInteractingState(
            IStateMachine stateMachine,
            InteractController interactController,
            CharacterAnimator animator
        )
        {
            this.stateMachine = stateMachine;
            this.interactController = interactController;
            this.animator = animator;
        }

        public void Enter()
        {
            stateMachine.ToggleBlockMovement(true);
            stateMachine.ToggleBlockAction(true);

            npcDialogClosedEvent.OnRaised += OnDialogClosed;

            interactController.OnInteract();
        }

        public void Exit()
        {
            npcDialogClosedEvent.OnRaised -= OnDialogClosed;

            stateMachine.ToggleBlockMovement(false);
            stateMachine.ToggleBlockAction(false);
        }

        private void OnDialogClosed()
        {
            stateMachine.SetActionState(null);
        }

        public void Dispose()
        {
            stateMachine = null;
            interactController = null;
            npcDialogClosedEvent = null;
            animator = null;
        }
    }
}
