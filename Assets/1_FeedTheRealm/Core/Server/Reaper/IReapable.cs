using System;

namespace FTR.Core.Server.Reaper
{
    public interface IReapable
    {
        // Determines if the entity can be reaped (destroyed) based on its interal state
        bool CanReap();

        // The time when the entity was spawned, used to determine how long it has been alive
        // and if it should be reaped
        DateTime SpawnTime { get; set; }

        // The minimum amount of time (in seconds) the entity should be alive before it can be reaped
        float MinimumLifetimeSeconds { get; }
    }
}
