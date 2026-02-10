using UnityEngine;

namespace FTR.Core.Common.Domain.Movement;

public static class MovementRules
{
    /// <summary>
    /// Calculates the next position based on current position, direction, speed, and delta time
    /// (Discrete integral).
    /// </summary>
    public static Vector3 CalculateNextPosition(
        Vector3 currentPosition,
        Vector3 direction,
        float moveSpeed,
        float deltaTime
    )
    {
        if (direction.sqrMagnitude < 0.01f) // No movement
            return currentPosition;

        return currentPosition + (direction * moveSpeed * deltaTime);
    }
}
