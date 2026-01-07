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
        interactComponent.OnInteract();
        Debug.Log("BLOCK");
    }

    public void Exit()
    {
        interactComponent.OnInteractFinished -= OnInteractFinished;
        stateMachine.ToggleBlockMovement(false);
        stateMachine.ToggleBlockAction(false);
        Debug.Log("UNBLOCK");
    }

    private void OnInteractFinished()
    {
        Debug.Log("Interaction finished.");
        stateMachine.SetActionState(null);
    }

    public void Dispose()
    {
        stateMachine = null;
        interactComponent = null;
        animator = null;
    }
}
