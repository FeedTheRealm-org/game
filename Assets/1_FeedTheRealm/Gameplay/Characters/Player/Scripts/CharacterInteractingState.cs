using UnityEngine;

/// <summary>
/// State for when the character is interacting with an object or npc.
/// </summary>
public class CharacterInteractingState : IState
{
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

    public void Enter()
    {
        _interactComponent.OnInteract();
    }

    public void Exit() { }
}
