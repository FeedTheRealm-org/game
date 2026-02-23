using FTR.Core.Server.Commands;
using FTR.Core.Server.Events;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters;

public class ServerCommandHandler : MonoBehaviour, ICommandable
{
    private MovementSystem movementSystem;

    public void Initialize(MovementSystem movementSystem)
    {
        this.movementSystem = movementSystem;
    }

    public void OnMove(IEventCollectable ec, Vector3 direction)
    {
        movementSystem.OnMove(direction);
    }

    public void OnDash(IEventCollectable ec) { }

    public void OnUse(IEventCollectable ec) { }

    public void OnInteract(IEventCollectable ec) { }

    public void OnEquip(IEventCollectable ec) { }

    public void OnDrop(IEventCollectable ec) { }

    public void OnPurchase(IEventCollectable ec) { }

    public void OnQuestAccepted(IEventCollectable ec) { }
}
