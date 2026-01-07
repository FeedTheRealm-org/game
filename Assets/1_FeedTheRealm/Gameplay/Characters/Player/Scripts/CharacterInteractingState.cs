using Game.Core.StateMachine;
using UnityEngine;

/// <summary>
/// State for when the character is interacting with an object or npc.
/// </summary>
public class CharacterInteractingState : IActionState
{
    private IStateMachine stateMachine;

    private PlayerInteractComponent interactComponent;
    private CharacterAnimator animator;

    public CharacterInteractingState(
        IStateMachine sm,
        PlayerInteractComponent interactComponent,
        CharacterAnimator animator
    )
    {
        this.interactComponent = interactComponent;
        this.animator = animator;
        this.stateMachine = sm;
    }

    public void Enter()
    {
        interactComponent.OnInteractFinished += OnInteractFinished;
        stateMachine.ToggleBlockMovement(true);
        stateMachine.ToggleBlockAction(true);
        if (!interactComponent.OnInteract())
        {
            stateMachine.ToggleBlockMovement(false);
            stateMachine.ToggleBlockAction(false);
        }
    }

    public void Exit()
    {
        interactComponent.OnInteractFinished -= OnInteractFinished;
        stateMachine.ToggleBlockMovement(false);
        stateMachine.ToggleBlockAction(false);
    }

    private void OnInteractFinished()
    {
        stateMachine.SetActionState(null);
    }

    public void Dispose()
    {
        stateMachine = null;
        interactComponent = null;
        animator = null;
    }
}
