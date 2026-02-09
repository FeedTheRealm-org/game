using UnityEngine;

namespace Game.Core.Client.Utils;

public static class VectorTransformations
{
    public static bool IsMovementMagnitude(Vector2 vec)
    {
        return vec.sqrMagnitude > 0.01f;
    }

    public static bool IsMovementMagnitude(Vector3 vec)
    {
        return vec.sqrMagnitude > 0.01f;
    }
}
