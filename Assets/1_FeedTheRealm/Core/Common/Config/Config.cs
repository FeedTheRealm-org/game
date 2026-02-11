using UnityEngine;

namespace FTR.Core.Common.Config
{
    public enum RuntimeRole
    {
        Server,
        Client,
    }

    [CreateAssetMenu(menuName = "Scriptable Objects/Config")]
    public class Config : ScriptableObject
    {
        [Header("Runtime Role")]
        [SerializeField]
        private RuntimeRole editorRuntimeRole = RuntimeRole.Client;

        public RuntimeRole RuntimeRole
        {
            get
            {
#if SERVER_BUILD
                return RuntimeRole.Server;
#elif CLIENT_BUILD
                return RuntimeRole.Client;
#else
                return editorRuntimeRole;
#endif
            }
        }
    }
}
