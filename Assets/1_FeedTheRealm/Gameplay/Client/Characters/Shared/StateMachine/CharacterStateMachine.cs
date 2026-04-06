using System.Collections.Generic;
using FTR.Core.Client.EventChannels.Interaction;
using FTR.Core.Client.Exceptions;
using FTR.Core.Client.StateMachine;
using FTR.Core.Common.EventChannels;
using FTR.Gameplay.Client.Characters.Shared.StateMachine.States;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Characters.Shared.StateMachine
{
    public class CharacterStateMachine : MonoBehaviour, IStateMachine
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private MovementController movementController;

        [SerializeField]
        private UseController useController;

        private InteractController interactController;

        [SerializeField]
        private CharacterAnimator characterAnimator;

        [Inject]
        private NpcDialogClosedEvent npcDialogClosedEvent;

        [Inject]
        private ShowQuestPromptEvent showQuestPromptEvent;

        [Inject]
        private QuestDecisionEvent questDecisionEvent;

        [Inject]
        private InteractFailedEvent interactFailedEvent;

        [Inject]
        private InteractCompletedEvent interactCompletedEvent;

        public readonly Dictionary<System.Type, IMovementState> movementStates =
            new Dictionary<System.Type, IMovementState>();
        public readonly Dictionary<System.Type, IActionState> actionStates =
            new Dictionary<System.Type, IActionState>();

        public IMovementState CurrentMovementState { get; private set; }
        public IActionState CurrentActionState { get; private set; }

        private Vector2 lastDirection;
        private bool isMovementBlocked;
        private bool isActionBlocked;
        private float _interactCooldownUntil;

        private void Awake()
        {
            if (movementController == null || useController == null || characterAnimator == null)
                throw new MissingFieldException(
                    "One or more required components are missing in CharacterStateMachine."
                );
        }

        public void Initialize(InteractController interactController)
        {
            this.interactController = interactController;

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
                        showQuestPromptEvent,
                        interactFailedEvent,
                        interactCompletedEvent,
                        characterAnimator
                    )
                );

                actionStates.Add(
                    typeof(CharacterQuestState),
                    new CharacterQuestState(this, questDecisionEvent, npcDialogClosedEvent)
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
            if (
                CurrentActionState is CharacterInteractingState
                || CurrentActionState is CharacterQuestState
            )
                return;

            SetActionState(actionStates[typeof(CharacterUsingState)]);
        }

        public void OnInteract()
        {
            if (CurrentActionState is CharacterQuestState)
                return;

            if (isActionBlocked)
                return;

            if (Time.time < _interactCooldownUntil)
                return;

            if (CurrentActionState is CharacterInteractingState interactingState)
            {
                interactingState.OnInteractWhileActive();
            }
            else
            {
                interactController.OnInteract();
                SetActionState(actionStates[typeof(CharacterInteractingState)]);
            }
        }

        public void OnDialogClosed()
        {
            _interactCooldownUntil = Time.time + 0.3f;
        }

        public void OnDialogNext()
        {
            if (CurrentActionState is CharacterInteractingState)
                interactController.OnDialogNext();
        }
    }
}
