using FTR.Core.Common.Enums;

namespace FTR.Core.Common.Protocol.RpcMessages;

/// <summary>
/// TransactionCommand represents a transaction command issued by the player to be used in networking.
/// </summary>
public struct TransactionCommandDTO
{
    public uint NetId;
    public string Id;
    public TransactionType Type;
}
