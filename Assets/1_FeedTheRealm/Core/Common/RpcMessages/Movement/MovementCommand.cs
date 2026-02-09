namespace Game.Core.Common.RpcMessages.Movement;

/// <summary>
/// Represents a movement command issued by the player.
/// </summary>
public struct MovementCommand
{
    public uint sequenceNumber;
    public float x;
    public float y;
    public float z;
}
