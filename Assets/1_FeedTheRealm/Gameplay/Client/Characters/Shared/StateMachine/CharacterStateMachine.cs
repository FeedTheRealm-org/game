using System.Collections.Generic;
using FTR.Core.Client.Exceptions;
using FTR.Core.Client.StateMachine;
using FTR.Core.Common.EventChannels;
using FTR.Gameplay.Client.Characters.Shared.StateMachine.States;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Characters.Shared.StateMachine
{
    /// <summary>
    /// Manages the character's state machine, handling transitions based on input and state.
    /// </summary>
    public class CharacterStateMachine : MonoBehaviour, IStateMachine
    {
        [SerializeField]
        private Logging.Logger logger;

        /* Components */
        [SerializeField]
        private MovementController movementController;

        [SerializeField]
        private UseController useController;

        [SerializeField]
        private InteractController interactController;

        [SerializeField]
        private CharacterAnimator characterAnimator;

        [Inject]
        private NpcDialogClosedEvent npcDialogClosedEvent;

        /* States */
        public readonly Dictionary<System.Type, IMovementState> movementStates =
            new Dictionary<System.Type, IMovementState>();
        public readonly Dictionary<System.Type, IActionState> actionStates =
            new Dictionary<System.Type, IActionState>();

        /* State Layers */
        public IMovementState CurrentMovementState { get; private set; }
        public IActionState CurrentActionState { get; private set; }

        private Vector2 lastDirection;
        private bool isMovementBlocked;
        private bool isActionBlocked;
        private float _interactCooldownUntil;

        private void Awake()
        {
            if (movementController == null || useController == null || characterAnimator == null)
            {
                throw new MissingFieldException(
                    "One or more required components are missing in CharacterStateMachine."
                );
            }
        }

        public void Initialize()
        {
            movementStates.Add(
                typeof(CharacterIdleState),
                new CharacterIdleState(this, movementController, characterAnimator)
            );
            movementStates.Add(
                typeof(CharacterMovingState),
                new CharacterMovingState(this, movementController)
            );
            movementStates.Add(
                typeof(CharacterDashingState),
                new CharacterDashingState(this, movementController)
            );
            actionStates.Add(
                typeof(CharacterUsingState),
                new CharacterUsingState(this, useController, characterAnimator)
            );
            if (npcDialogClosedEvent != null)
            {
                actionStates.Add(
                    typeof(CharacterInteractingState),
                    new CharacterInteractingState(
                        this,
                        interactController,
                        npcDialogClosedEvent,
                        characterAnimator
                    )
                );
            }

            SetMovementState(movementStates[typeof(CharacterIdleState)]);
        }

        private void OnDestroy()
        {
            foreach (var state in movementStates.Values)
                state.Dispose();
            movementStates.Clear();

            foreach (var state in actionStates.Values)
                state.Dispose();
            actionStates.Clear();
        }

        public void SetMovementState(IMovementState newState)
        {
            CurrentMovementState?.Exit();
            CurrentMovementState = newState;
            CurrentMovementState?.Enter();
            if (!isMovementBlocked)
                CurrentMovementState.SetDirection(lastDirection);
        }

        public void SetActionState(IActionState newState)
        {
            CurrentActionState?.Exit();
            CurrentActionState = newState;
            CurrentActionState?.Enter();
        }

        public void ToggleBlockMovement(bool shouldBlock)
        {
            isMovementBlocked = shouldBlock;
            if (isMovementBlocked)
                SetMovementState(movementStates[typeof(CharacterIdleState)]);
        }

        public void ToggleBlockAction(bool shouldBlock)
        {
            isActionBlocked = shouldBlock;
        }

        public IMovementState GetMovementStateByType(System.Type type)
        {
            movementStates.TryGetValue(type, out IMovementState state);
            return state;
        }

        public IActionState GetActionStateByType(System.Type type)
        {
            actionStates.TryGetValue(type, out IActionState state);
            return state;
        }

        public void OnMove(Vector2 direction)
        {
            if (isMovementBlocked)
                return;
            lastDirection = direction;
            CurrentMovementState.SetDirection(direction);
        }

        public void OnDash()
        {
            if (isMovementBlocked)
                return;
            SetMovementState(movementStates[typeof(CharacterDashingState)]);
        }

        public void OnUse()
        {
            if (isActionBlocked)
                return;
            SetActionState(actionStates[typeof(CharacterUsingState)]);
        }

        /// <summary>
        /// Always dispatches Interact to the server.
        /// The server authoritatively decides whether to start, advance, switch, or close dialog.
        /// A short cooldown prevents re-opening a dialog immediately after closing it.
        /// </summary>
        public void OnInteract()
        {
            if (isActionBlocked)
                return;

            if (UnityEngine.Time.time < _interactCooldownUntil)
                return;

            interactController.OnInteract();

            if (CurrentActionState is not CharacterInteractingState)
                SetActionState(actionStates[typeof(CharacterInteractingState)]);
        }

        /// <summary>
        /// Called by CharacterInteractingState when the dialog closes,
        /// to set a cooldown that prevents immediately re-opening it.
        /// </summary>
        public void OnDialogClosed()
        {
            _interactCooldownUntil = UnityEngine.Time.time + 0.3f;
        }

        /// <summary>
        /// Only forwards DialogNext if currently interacting (kept for external callers).
        /// </summary>
        public void OnDialogNext()
        {
            if (CurrentActionState is CharacterInteractingState)
                interactController.OnDialogNext();
        }
    }
}
