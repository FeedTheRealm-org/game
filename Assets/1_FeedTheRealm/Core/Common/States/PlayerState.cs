using UnityEngine;

namespace FTR.Core.Common.States;

public class PlayerState
{
    public Vector3 LastPosition { get; set; }

    public PlayerState(Vector3 lastPosition)
    {
        LastPosition = lastPosition;
    }
}
