using Game.Core.Interactions;
using UnityEngine;

public class NpcInteractComponent : MonoBehaviour, IInteractable
{
    [Header("Dialog settings")]
    [SerializeField]
    private DialogManagerComponent _dialogManager;

    public void Interact(IInteractor interactor)
    {
        _dialogManager.Next();
        Debug.Log("NPC interacted with by " + interactor.GameObject.name);
        interactor.FinishInteracting();
    }

    public bool CanInteract(IInteractor interactor)
    {
        return true;
    }
}
