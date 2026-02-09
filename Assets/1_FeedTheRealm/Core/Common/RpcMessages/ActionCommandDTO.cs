using Game.Core.Client.Enum;
using UnityEngine;

namespace Game.Core.Common.RpcMessages;

/// <summary>
/// ActionCommand represents an action command issued by the player, and to be used in networking.
/// </summary>
public struct ActionCommandDTO
{
    public Vector3 Direction;
    public ActionType Type;
}
