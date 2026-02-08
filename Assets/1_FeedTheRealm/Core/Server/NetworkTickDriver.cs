public class NetworkTickDriver
{
    readonly NetworkService network;

    public NetworkTickDriver(NetworkService network)
    {
        this.network = network;
    }

    public void TickBefore()
    {
        // This is where we would have received packets from NetworkAdapters
        // and build Commands to send to GameLoop via CommandQueue.
        // As for now they are directly sent to NetworkService via events.
    }

    public void TickAfter()
    {
        network.FlushEventsToClients();
    }
}
