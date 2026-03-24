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

        public virtual async UniTask Load(WorldData worldData)
        {
            Debug.Log($"[ServerItemLoader] Loading world items for world: {worldData.id}");
            ServerItemsRegistry.RegisterWorldData(worldData);
        }
    }
}
