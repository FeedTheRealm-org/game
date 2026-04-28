using FTR.Core.Common.Utils;
using UnityEngine;

namespace FTR.Core.Common.Config
{
    public enum RuntimeRole
    {
        Server,
        Client,
        Bot,
    }

    [CreateAssetMenu(menuName = "Scriptable Objects/Config/Config")]
    public class Config : ScriptableObject
    {
        [Header("Runtime Role")]
        [SerializeField]
        private RuntimeRole _editorRuntimeRole = RuntimeRole.Client;
        public RuntimeRole RuntimeRole => GetRuntimeRole();

        public int MaxWorldLoadRetries = 10;
        public int WorldLoadRetryDelayMs = 1000;

        [Header("Server Port Settings")]
        // Server port settings are here because FTRNetworkManager is *Common*
        [SerializeField]
        private ushort _listeningPort = 7777;
        public ushort ListeningPort =>
            ushort.Parse(ParamsSerializer.GetArgs("port", _listeningPort.ToString()));

        [SerializeField]
        private ushort _healthcheckPort = 7778;
        public ushort HealthcheckPort =>
            ushort.Parse(ParamsSerializer.GetArgs("hport", _healthcheckPort.ToString()));

        [Header("Test Settings")]
        // Even if this is set, BOTS will not be allowed to join unless server starts with `--allow-bots=true`
        public string BotJoinToken = "test_join_token";
        public bool EnableActionLogging = false;

        [Header("API Settings")]
        public ApiConfig ApiConfig;

        [Header("Common Layer Masks")]
        public LayerMask CubeColliderLayerMask;
        public LayerMask SlopeColliderLayerMask;

        [Header("Client Connection Settings")] // TODO: move to client & bot config
        public string CurrentServerAddress = "";
        public ushort CurrentServerPort = 10000;

#if DEBUG
        [Header("Debug Settings")]
        public bool DEBUG_IsDebugWorld = false;
        public bool DEBUG_DoNotLoadWorld = false;
        public string DEBUG_WorldId = string.Empty;
        public int DEBUG_ZoneId = 0;
        public bool DEBUG_EnableColliderView = false;
#else
        public bool DEBUG_IsDebugWorld => false;
        public bool DEBUG_DoNotLoadWorld => false;
        public string DEBUG_WorldId => string.Empty;
        public int DEBUG_ZoneId => 0;
        public bool DEBUG_EnableColliderView => false;
#endif

        private RuntimeRole GetRuntimeRole()
        {
#if SERVER_BUILD && !CLIENT_BUILD && !BOT_BUILD
            return RuntimeRole.Server;
#elif CLIENT_BUILD && !SERVER_BUILD && !BOT_BUILD
            return RuntimeRole.Client;
#elif BOT_BUILD && !SERVER_BUILD && !CLIENT_BUILD
            return RuntimeRole.Bot;
#else // SERVER & CLIENT || None (debugging)
            return _editorRuntimeRole;
#endif
        }
    }
}
