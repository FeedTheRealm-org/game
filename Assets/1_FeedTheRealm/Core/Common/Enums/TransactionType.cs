namespace FTR.Core.Common.Enums;

/// <summary>
/// TransactionType represents different types of transactions a character can perform.
/// </summary>
public enum TransactionType
{
    SetUserId,
    EquipItem,
    DropItem,
    Purchase,
    PickUp,
    AcceptQuest,
    RejectQuest,
    MoveItem,
    SendMessage,
}
