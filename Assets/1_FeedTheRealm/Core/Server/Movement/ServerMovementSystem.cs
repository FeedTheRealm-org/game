using Game.Core.Domain.Movement;
using UnityEngine;

namespace Game.Core.Server.Movement;

public static class ServerMovementSystem
{
    public static MovementSnapshot ProcessMovementCommand(
        Transform transform,
        MovementCommand command,
        float moveSpeed,
        float deltaTime
    )
    {
        Vector3 direction = new Vector3(command.x, command.y, command.z);
        Vector3 nextPosition = MovementRules.CalculateNextPosition(
            transform.position,
            direction,
            moveSpeed,
            deltaTime
        );
        transform.position = nextPosition;

        return new MovementSnapshot
        {
            sequenceNumber = command.sequenceNumber,
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z,
        };
    }
}
