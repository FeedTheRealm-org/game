using Mirror;

namespace FTR.Core.Client.Settings
{
    public interface INetworkStats
    {
        bool IsConnected { get; }
        double RttMilliseconds { get; }
    }

    public class MirrorNetworkStats : INetworkStats
    {
        public bool IsConnected => NetworkClient.active;
        public double RttMilliseconds => NetworkTime.rtt * 1000.0;
    }
}
