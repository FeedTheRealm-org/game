using FTR.Core.Common.Enums;
using UnityEngine;

namespace FTR.Core.Common.Protocol.RpcMessages;

/// <summary>
/// ActionCommand represents an action command issued by the player, and to be used in networking.
/// </summary>
public struct ActionCommandDTO
{
    public uint NetId;
    public Vector3 Direction;
    public ActionType Type;
}
