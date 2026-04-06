using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Server.Registry;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Loaders
{
    public class ServerItemLoader : MonoBehaviour, ILoader
    {
        public ServerItemLoader() { }

        public virtual async UniTask Load(
            string worldId,
            ZoneData zoneData,
            CreatablesData creatablesData
        )
        {
            Debug.Log($"[ServerItemLoader] Loading world items for world: {worldId}");
            ServerItemsRegistry.RegisterWorldData(creatablesData);
        }
    }
}
