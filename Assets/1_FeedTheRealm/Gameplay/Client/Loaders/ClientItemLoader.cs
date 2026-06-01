using Cysharp.Threading.Tasks;
using FeedTheRealm.Core.EventChannels.Setup;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Client.Registry;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Loaders
{
    public class ClientItemLoader : ILoader
    {
        [Inject]
        private LoadingProgressEvent loadingProgressEvent;

        public ClientItemLoader() { }

        public virtual async UniTask Load(
            string worldId,
            ZoneData zoneData,
            CreatablesData creatablesData
        )
        {
            Debug.Log($"[ClientItemLoader] Loading world items for world: {worldId}");
            ClientItemsRegistry.RegisterWorldData(creatablesData);
            loadingProgressEvent?.Raise(0.9f);
        }
    }
}
