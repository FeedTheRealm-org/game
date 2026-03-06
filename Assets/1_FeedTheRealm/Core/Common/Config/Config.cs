using FTR.Core.Common.Utils;
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
