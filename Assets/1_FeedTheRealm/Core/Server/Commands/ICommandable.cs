namespace FTR.Core.Server.Commands;

public interface ICommandable
{
    void OnMove();
    void OnDash();
    void OnUse();
    void OnInteract();
    void OnEquip();
    void OnDrop();
    void OnPurchase();
    void OnQuestAccepted();
}
