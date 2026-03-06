using FTR.Core.Server.Commands;
using FTR.Core.Server.Entities;
using FTR.Core.Server.Events;
using FTR.Core.Server.States;

public sealed class WorldMonitor
{
    public CommandQueue Commands { get; } = new();
    public EventQueue Events { get; } = new();
    public EntityRegistry Entities { get; } = new();
    public LatestStateStore LatestState { get; } = new();
}
