using Game.Core.Common.Domain.Movement;
using Game.Core.Common.RpcMessages.Movement;
using UnityEngine;

namespace Game.Core.Server.Movement;

public static class ServerMovementSystem
{
    public static MovementSnapshot ProcessMovementCommand(
        Rigidbody rb,
        MovementCommand command,
        float moveSpeed,
        float deltaTime
    )
    {
        Vector3 direction = new Vector3(command.x, command.y, command.z);
        Vector3 nextPosition = MovementRules.CalculateNextPosition(
            rb.position,
            direction,
            moveSpeed,
            deltaTime
        );

        return new MovementSnapshot
        {
            sequenceNumber = command.sequenceNumber,
            x = nextPosition.x,
            y = nextPosition.y,
            z = nextPosition.z,
        };
    }
}
