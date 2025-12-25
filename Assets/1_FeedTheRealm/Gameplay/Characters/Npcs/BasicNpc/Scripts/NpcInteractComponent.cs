using Game.Core.Interactions;
using UnityEngine;

public class NpcInteractComponent : MonoBehaviour, IInteractable
{
    public void Interact(IInteractor interactor)
    {
        Debug.Log("NPC interacted with by " + interactor.GameObject.name);
    }

    public bool CanInteract(IInteractor interactor)
    {
        return true;
    }
}
