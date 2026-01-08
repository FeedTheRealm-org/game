using UnityEngine;

namespace Game.Core.Utils
{
    public static class VectorTransformations
    {
        public static bool IsMovementMagnitude(Vector2 vec)
        {
            return vec.sqrMagnitude > 0.01f;
        }
    }
}
