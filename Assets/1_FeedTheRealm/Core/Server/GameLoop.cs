using UnityEngine;

public class GameLoop
{
    private readonly WorldMonitor worldMonitor;

    public GameLoop(WorldMonitor worldMonitor)
    {
        this.worldMonitor = worldMonitor;
    }

    public void TickOnce(float dt)
    {
        // TODO: Get commands from CommandQueue, process game logic,
        // and push resulting events to NetworkQueue and update the LatestState for
        // corresponding networked entities.
        // Call Physics.Simulate.

        // Pop cmd
        // Apply cmd
        // Update/Tick entities such as rb.MovePosition, etc.

        Physics.Simulate(dt);

        // Post simulation checks? e.g. ground check?
        // Push events to NetworkQueue
        // Update LatestState for networked entities
    }
}
