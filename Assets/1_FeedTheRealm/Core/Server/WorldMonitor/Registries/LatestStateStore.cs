using System.Collections.Generic;
using FTR.Core.Server.States;

public sealed class LatestStateStore
{
    private readonly Dictionary<uint, EntityState> states = new();

    public void Set(uint netId, EntityState state)
    {
        states[netId] = state;
    }

    public IReadOnlyDictionary<uint, EntityState> Snapshot()
    {
        return states;
    }
}
