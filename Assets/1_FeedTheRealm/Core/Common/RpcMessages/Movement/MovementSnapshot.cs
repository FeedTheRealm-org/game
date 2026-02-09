namespace Game.Core.Common.RpcMessages.Movement;

/// <summary>
/// Represents a position snapshot issued by the server.
/// </summary>
public struct MovementSnapshot
{
    public uint sequenceNumber;
    public float x;
    public float y;
    public float z;
}
