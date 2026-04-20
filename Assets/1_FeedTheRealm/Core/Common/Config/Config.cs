using FTR.Core.Common.Utils;
using FTR.Core.Server.Utils;
using UnityEngine;

namespace FTR.Core.Common.Config
{
    public enum RuntimeRole
    {
        Server,
        Client,
    }

    [CreateAssetMenu(menuName = "Scriptable Objects/Config/Config")]
    public class Config : ScriptableObject
    {
        /* PROPERTIES */

        [Header("Runtime Role")]
        [SerializeField]
        private RuntimeRole editorRuntimeRole = RuntimeRole.Client;
        public RuntimeRole RuntimeRole
        {
            get => GetRuntimeRole();
        }

        public int MaxWorldLoadRetries = 10;
        public int WorldLoadRetryDelayMs = 1000;

        [Header("API Settings")]
        public ApiConfig ApiConfig;

        [Header("Server Settings")]
        private ushort? _listeningPort = 7777;
        public ushort ListeningPort =>
            ushort.Parse(ParamsSerializer.GetArgs("port", _listeningPort?.ToString()));

        [SerializeField]
        private ushort? _healthcheckPort = 7778;
        public ushort HealthcheckPort =>
            ushort.Parse(ParamsSerializer.GetArgs("hport", _healthcheckPort?.ToString()));

#if SERVER_BUILD || DEBUG
        [SerializeField]
        private string serverAccessToken = "test_token";
        public string ServerAccessToken => serverAccessToken;
#else
        public string ServerAccessToken => string.Empty;
#endif

        [Header("Client Connection Settings")]
        public string CurrentServerAddress = "";
        public ushort CurrentServerPort = 10000;

#if DEBUG
        [Header("Debug Settings")]
        [SerializeField]
        private bool isDebugWorld = false;
        public bool IsDebugWorld => isDebugWorld;

        [SerializeField]
        private bool doNotLoadWorld = false;
        public bool DoNotLoadWorld => doNotLoadWorld;

        [SerializeField]
        private string worldID = "world_1";
        public string WorldID => worldID;

        [SerializeField]
        private int zoneID = 1;
        public int ZoneID => zoneID;
#else
        public bool IsDebugWorld => false;
        public bool DoNotLoadWorld => false;
        public string WorldID => string.Empty;
        public int ZoneID => 0;
#endif

        /* HELPERS */

        private RuntimeRole GetRuntimeRole()
        {
#if SERVER_BUILD && !CLIENT_BUILD
            return RuntimeRole.Server;
#elif CLIENT_BUILD && !SERVER_BUILD
            return RuntimeRole.Client;
#else // SERVER & CLIENT || None (debugging)
            return editorRuntimeRole;
#endif
        }
    }
}
