using System;
using FTR.Core.Server.Events;
using UnityEngine;

namespace FTR.Core.Server.Commands;

public interface ICommandable
{
    void OnMove(IEventCollectable ec, Vector3 direction);
    void OnDash(IEventCollectable ec);
    void OnUse(IEventCollectable ec);
    void OnInteract(IEventCollectable ec);
    void OnEquip(IEventCollectable ec);
    void OnDrop(IEventCollectable ec);
    void OnPurchase(IEventCollectable ec);
    void OnQuestAccepted(IEventCollectable ec);
    void OnPickUp(IEventCollectable ec, string itemId, Action<bool> onComplete);
}
