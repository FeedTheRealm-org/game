using Game.Core.Server.Commands;
using Game.Core.Server.Entities;
using Game.Core.Server.Events;
using Game.Core.Server.States;

public sealed class WorldMonitor
{
    public CommandQueue Commands { get; } = new();
    public EventQueue Events { get; } = new();
    public LatestStateStore LatestState { get; } = new();
    public EntityRegistry Entities { get; } = new();
}
