namespace FTR.Core.Server.Commands;

public interface ICommandable
{
    void OnMove();
    void OnUse();
    void OnInteract();
    void OnEquip();
    void OnDrop();
    void OnPurchase();
    void OnQuestAccepted();
}
