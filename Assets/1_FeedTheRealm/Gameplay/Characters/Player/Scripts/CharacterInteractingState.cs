using Game.Core.StateMachine;
using UnityEngine;

/// <summary>
/// State for when the character is interacting with an object or npc.
/// </summary>
public class CharacterInteractingState : IActionState
{
    private IStateMachine stateMachine;

    private PlayerInteractComponent _interactComponent;
    private CharacterAnimator _animator;

    public CharacterInteractingState(
        PlayerInteractComponent movementComponent,
        CharacterAnimator animator
    )
    {
        _interactComponent = movementComponent;
        _animator = animator;
    }

    public void Enter(IStateMachine stateMachine)
    {
        _interactComponent.OnInteractFinished += OnInteractFinished;
        _interactComponent.OnInteract();

        if (this.stateMachine == null)
        {
            this.stateMachine = stateMachine;
        }
    }

    public void Exit(IStateMachine stateMachine)
    {
        _interactComponent.OnInteractFinished -= OnInteractFinished;
        if (this.stateMachine != null)
            this.stateMachine = null;
    }

    private void OnInteractFinished()
    {
        stateMachine?.SetActionState(null);
    }
}
