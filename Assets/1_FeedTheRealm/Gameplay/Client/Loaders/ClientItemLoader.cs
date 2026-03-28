using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Client.Registry;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Loaders
{
    public class ClientItemLoader : ILoader
    {
        public ClientItemLoader() { }

        public virtual async UniTask Load(WorldData worldData)
        {
            Debug.Log($"[ClientItemLoader] Loading world items for world: {worldData.id}");
            ClientItemsRegistry.RegisterWorldData(worldData);
        }
    }
}
