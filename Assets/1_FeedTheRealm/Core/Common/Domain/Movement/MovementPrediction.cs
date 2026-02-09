using System.Collections.Generic;
using Game.Core.Common.RpcMessages.Movement;
using UnityEngine;

namespace Game.Core.Common.Domain.Movement;

public class MovementPrediction
{
    private readonly Queue<MovementCommand> commandQueue = new Queue<MovementCommand>();

    /// <summary>
    /// Stores a new movement command for future reconciliation.
    /// </summary>
    public void Store(MovementCommand command)
    {
        commandQueue.Enqueue(command);
    }

    /// <summary>
    /// Reconciles the current transform position with the given snapshot and applies stored commands.
    /// </summary>
    public void Reconcile(
        Transform transform,
        MovementSnapshot snapshot,
        float speed,
        float deltaTime
    )
    {
        // Reset to snapshot state
        transform.position = new Vector3(snapshot.x, snapshot.y, snapshot.z);

        while (
            commandQueue.Count > 0 && commandQueue.Peek().sequenceNumber <= snapshot.sequenceNumber
        )
            commandQueue.Dequeue(); // Remove old commands

        // Apply newer commands (if any)
        foreach (var command in commandQueue)
        {
            Vector3 direction = new Vector3(command.x, command.y, command.z);
            Vector3 nextPosition = MovementRules.CalculateNextPosition(
                transform.position,
                direction,
                speed,
                deltaTime
            );
            transform.position = nextPosition;
        }
    }
}
