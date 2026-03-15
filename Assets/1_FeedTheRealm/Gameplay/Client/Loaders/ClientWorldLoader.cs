using FTR.Gameplay.Client.EntryPoints;
using FTR.Gameplay.Common.LoaderEntities;
using UnityEngine;

namespace FTR.Gameplay.Client.Loaders
{
    public class ClientWorldLoader : WorldLoaderManager
    {
        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private WorldSelector worldSelector;

        public override string GetWorldId()
        {
            if (config.IsDebugWorld)
                return !string.IsNullOrEmpty(config.WorldID)
                    ? config.WorldID
                    : worldSelector.GetSelectedWorldId();
            return worldSelector.GetSelectedWorldId();
        }

        public override string GetAccessToken()
        {
            if (config.IsDebugWorld)
                return !string.IsNullOrEmpty(config.ServerAccessToken)
                    ? config.ServerAccessToken
                    : session.APIToken;
            return session.APIToken;
        }
    }
}
