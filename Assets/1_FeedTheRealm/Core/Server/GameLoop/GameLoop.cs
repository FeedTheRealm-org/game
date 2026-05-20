using System;
using System.Diagnostics;
using API;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.Entities;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Core.Server.Metrics;
using FTR.Core.Server.Metrics;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// GameLoop is responsible for processing game logic,
/// and should be called in a fixed update loop, **RUNS IN MAIN THREAD**.
/// </summary>
public class GameLoop : IGameTickable, IStartable
{
    private readonly WorldMonitor worldMonitor;
    private readonly GameTickEvent gameTickEvent;
    private Logging.Logger logger;
    private readonly ServerConfig serverConfig;
    private readonly PlayerStatsSender playerStatsSender;

    private readonly long maxCommandsTimePerTick = Stopwatch.Frequency / 1000 * 10; // 10ms
    private readonly long maxCommandsPerTick = 100;

    private readonly EventCollector eventCollector = new();

    private Action<float> _tickHandler;

    // Metrics
    private readonly Stopwatch _sw = new Stopwatch();
    private readonly Stopwatch _swPlayerCount = new Stopwatch();
    private readonly Stopwatch _sectionSw = new Stopwatch();
    private int _lastGen0,
        _lastGen1,
        _lastGen2;

    public GameLoop(
        WorldMonitor worldMonitor,
        GameTickEvent gameTickEvent,
        Logging.Logger logger,
        ServerConfig serverConfig,
        PlayerStatsSender playerStatsSender
    )
    {
        this.worldMonitor = worldMonitor;
        this.gameTickEvent = gameTickEvent;
        this.logger = logger;
        this.serverConfig = serverConfig;
        this.playerStatsSender = playerStatsSender;

        this._tickHandler = RegularGameTick;
    }

    public void Start()
    {
        this._tickHandler = serverConfig.IsTestWorld ? TestGameTick : RegularGameTick;
        logger.Log(
            $"GameLoop initialized with tick handler: {_tickHandler.Method.Name}",
            Logging.LogType.Info
        );
        _swPlayerCount.Restart();
    }

    public void GameTick(float dt)
    {
        _tickHandler(dt);
    }

    private void RegularGameTick(float dt)
    {
        _sw.Restart();

        ProcessCommands();

        Physics.Simulate(dt);
        gameTickEvent.Raise(dt);

        // Push new events to NetworkQueue
        eventCollector.ForEach(serverEvent => worldMonitor.Events.Enqueue(serverEvent));
        eventCollector.Clear();

        if (_swPlayerCount.Elapsed.TotalSeconds > serverConfig.SendPlayerCountIntervalSeconds)
        {
            logger.Log(
                $"[Gameloop] {_swPlayerCount.Elapsed.TotalSeconds} > {serverConfig.SendPlayerCountIntervalSeconds}"
            );
            _swPlayerCount.Restart();
            playerStatsSender.Send();
        }

        _sw.Stop();

        if (_sw.Elapsed.TotalMilliseconds > 20) // delayed tick
            logger.Log(
                $"[DELAYED TICK] total={_sw.Elapsed.TotalMilliseconds:F2}ms",
                Logging.LogType.Warning
            );
    }

    private void TestGameTick(float dt)
    {
        _sw.Restart();
        _sectionSw.Restart();
        ProcessCommands();
        _sectionSw.Stop();
        double processCommandsMs = _sectionSw.Elapsed.TotalMilliseconds;

        _sectionSw.Restart();
        Physics.Simulate(dt);
        _sectionSw.Stop();
        double physicsMs = _sectionSw.Elapsed.TotalMilliseconds;

        _sectionSw.Restart();
        gameTickEvent.Raise(dt);
        _sectionSw.Stop();
        double gameTickEventMs = _sectionSw.Elapsed.TotalMilliseconds;

        // Push new events to NetworkQueue
        _sectionSw.Restart();
        eventCollector.ForEach(serverEvent => worldMonitor.Events.Enqueue(serverEvent));
        eventCollector.Clear();
        _sectionSw.Stop();
        double eventEnqueuedMs = _sectionSw.Elapsed.TotalMilliseconds;

        _sw.Stop();
        double totalMs = _sw.Elapsed.TotalMilliseconds;

        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);

        int dGen0 = gen0 - _lastGen0;
        int dGen1 = gen1 - _lastGen1;
        int dGen2 = gen2 - _lastGen2;
        _lastGen0 = gen0;
        _lastGen1 = gen1;
        _lastGen2 = gen2;

        bool gcHappened = dGen0 > 0 || dGen1 > 0 || dGen2 > 0;

        bool slowTick = totalMs > 20;
        if (slowTick)
        {
            logger.Log(
                $"[TICK] total={totalMs}ms "
                    + $"commands={processCommandsMs:F2}ms "
                    + $"physics={physicsMs:F2}ms "
                    + $"tickEvent={gameTickEventMs:F2}ms"
                    + (gcHappened ? $" | [GC] d0={dGen0} d1={dGen1} d2={dGen2}" : "")
            );
        }

        DogStatsd.Histogram("server.tick_duration_ms", totalMs);
        DogStatsd.Histogram("server.commands_ms", processCommandsMs);
        DogStatsd.Histogram("server.physics_ms", physicsMs);
        DogStatsd.Histogram("server.tick_event_ms", gameTickEventMs);
        DogStatsd.Histogram("server.events_enqueued_ms", eventEnqueuedMs);
        DogStatsd.Gauge("server.gc.gen0", dGen0);
        DogStatsd.Gauge("server.gc.gen1", dGen1);
        DogStatsd.Gauge("server.gc.gen2", dGen2);
        DogStatsd.Gauge("server.entities_count", worldMonitor.Entities.Count);
        DogStatsd.Gauge("server.players_count", worldMonitor.Entities.PlayerCount);
        DogStatsd.Gauge("server.commands_queue_length", worldMonitor.Commands.Count);
        DogStatsd.Gauge("server.events_queue_length", worldMonitor.Events.Count);
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
