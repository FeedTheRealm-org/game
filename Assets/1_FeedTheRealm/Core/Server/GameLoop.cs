using System.Diagnostics;
using UnityEngine;

/// <summary>
/// GameLoop is responsible for processing game logic,
/// and should be called in a fixed update loop, **RUNS IN MAIN THREAD**.
/// </summary>
public class GameLoop
{
    private readonly WorldMonitor worldMonitor;

    private readonly long maxCommandsTimePerTick = Stopwatch.Frequency / 1000 * 10;
    private readonly long maxCommandsPerTick = 100;

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

        ProcessCommands();

        // Update/Tick entities such as rb.MovePosition, etc.

        Physics.Simulate(dt);

        // Post simulation checks? e.g. ground check?
        // Push events to NetworkQueue
        // States will be updated by syncvars
    }

    /// <summary>
    /// Poll & apply commands from CommandQueue, with a time and
    /// count limit per tick to avoid long processing time.
    /// </summary>
    private void ProcessCommands()
    {
        long start = Stopwatch.GetTimestamp();
        int processedThisTick = 0;

        while (
            processedThisTick < maxCommandsPerTick
            && (Stopwatch.GetTimestamp() - start) < maxCommandsTimePerTick
            && worldMonitor.Commands.TryDequeue(out var cmd)
        )
        {
            var entity = worldMonitor.Entities.Get(cmd.NetId);
            cmd.Apply(entity.Commandable);
            processedThisTick++;
        }
    }
}
