namespace FTR.Core.Server.Commands;

public abstract class BaseCommandHandler : ICommandable
{
    public virtual void OnMove() { }

    public virtual void OnDash() { }

    public virtual void OnUse() { }

    public virtual void OnInteract() { }

    public virtual void OnEquip() { }

    public virtual void OnDrop() { }

    public virtual void OnPurchase() { }

    public virtual void OnQuestAccepted() { }
}
