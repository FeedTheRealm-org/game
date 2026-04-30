using Mirror;

namespace FTR.Gameplay.Server.Characters;

/// <summary>
/// Ensures the entity is unregistered when destroyed
/// </summary>
public class ServerEntityCleanupTracker : NetworkBehaviour
{
    private WorldMonitor worldMonitor;
    private uint trackedNetId;

    public void Initialize(WorldMonitor world, uint netId)
    {
        worldMonitor = world;
        trackedNetId = netId;
    }

    public void OnDestroy()
    {
        Cleanup();
    }

    public override void OnStopServer()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        worldMonitor?.Entities.Unregister(trackedNetId);
    }
}
