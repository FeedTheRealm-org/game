public class NetworkTickDriver
{
    readonly NetworkService network;

    public NetworkTickDriver(NetworkService network)
    {
        this.network = network;
    }

    public void TickBefore()
    {
        network.Poll();
    }

    public void TickAfter()
    {
        network.Flush();
    }
}
