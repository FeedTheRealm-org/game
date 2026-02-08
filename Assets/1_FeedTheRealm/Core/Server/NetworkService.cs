public class NetworkService
{
    private readonly WorldMonitor worldMonitor;

    public NetworkService(WorldMonitor worldMonitor)
    {
        this.worldMonitor = worldMonitor;
    }

    public void Poll()
    {
        // TODO: Receive packets from NetworkAdapter build Commands and
        // send them to GameLoop via CommandQueue
    }

    public void Flush()
    {
        // TODO: Collect packets from GameLoop via NetworkQueue and
        // send them to correspoinding NetworkAdapters
    }
}
