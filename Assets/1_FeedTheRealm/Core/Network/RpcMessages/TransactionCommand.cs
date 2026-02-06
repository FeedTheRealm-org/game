using Game.Core.Enum;

namespace Game.Core.RpcMessages;

/// <summary>
/// TransactionCommand represents a transaction command issued by the player to be used in networking.
/// </summary>
public struct TransactionCommandDTO
{
    public string Id;
    public TransactionType Type;
}
