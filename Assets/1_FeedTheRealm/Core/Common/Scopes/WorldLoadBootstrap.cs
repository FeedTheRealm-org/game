namespace FTR.Core.Common.Scopes
{
    public static class WorldLoadBootstrap
    {
        public static bool ClientReady { get; private set; }
        public static bool ClientFailed { get; private set; }
        public static bool ServerReady { get; private set; }
        public static bool ServerFailed { get; private set; }

        public static void Reset()
        {
            ClientReady = false;
            ClientFailed = false;
            ServerReady = false;
            ServerFailed = false;
        }

        public static void MarkClientReady()
        {
            ClientReady = true;
            ClientFailed = false;
        }

        public static void MarkClientFailed()
        {
            ClientReady = false;
            ClientFailed = true;
        }

        public static void MarkServerReady()
        {
            ServerReady = true;
            ServerFailed = false;
        }

        public static void MarkServerFailed()
        {
            ServerReady = false;
            ServerFailed = true;
        }
    }
}
