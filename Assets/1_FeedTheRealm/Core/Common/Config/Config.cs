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

        public ApiConfig ApiConfig;

        public ushort Port = ushort.Parse(ParamsSerializer.GetArgs("port", "7777"));

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
        private string accessToken = "test_token";
        public string AccessToken => accessToken;
#else
        public bool IsDebugWorld => false;
        public bool DoNotLoadWorld => false;
        public string WorldID => string.Empty;
        public string AccessToken => string.Empty;
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
