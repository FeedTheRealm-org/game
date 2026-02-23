using System.Diagnostics;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Entities;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using UnityEngine;

/// <summary>
/// GameLoop is responsible for processing game logic,
/// and should be called in a fixed update loop, **RUNS IN MAIN THREAD**.
/// </summary>
public class GameLoop : IGameTickable
{
    private readonly WorldMonitor worldMonitor;

    private readonly long maxCommandsTimePerTick = Stopwatch.Frequency / 1000 * 10; // 10ms
    private readonly long maxCommandsPerTick = 100;

    private readonly EventCollector eventCollector = new();

    private readonly GameTickEvent gameTickEvent;

    public GameLoop(WorldMonitor worldMonitor, GameTickEvent gameTickEvent)
    {
        this.worldMonitor = worldMonitor;
        this.gameTickEvent = gameTickEvent;
    }

    public void GameTick(float dt)
    {
        ProcessCommands();

        // Update/Tick entities such as rb.MovePosition, etc.
        worldMonitor.Entities.Foreach(entity => entity.GameTickable.GameTick(dt));

        Physics.Simulate(dt);
        gameTickEvent.Raise(dt);

        // Push new events to NetworkQueue
        eventCollector.ForEach(serverEvent => worldMonitor.Events.Enqueue(serverEvent));
        eventCollector.Clear();

        // TODO: States will be updated by syncvars
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
            if (worldMonitor.Entities.TryGet(cmd.NetId, out ServerEntity entity))
            {
                cmd.Apply(entity.Commandable, eventCollector);
                processedThisTick++;
            }
        }
    }
}
