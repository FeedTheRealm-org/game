using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public interface ICommandable
{
    void OnMove(IEventCollectable ec);
    void OnDash(IEventCollectable ec);
    void OnUse(IEventCollectable ec);
    void OnInteract(IEventCollectable ec);
    void OnEquip(IEventCollectable ec);
    void OnDrop(IEventCollectable ec);
    void OnPurchase(IEventCollectable ec);
    void OnQuestAccepted(IEventCollectable ec);
}
