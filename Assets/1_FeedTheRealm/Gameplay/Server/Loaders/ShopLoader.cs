using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.Environment.Shop;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Loaders
{
    public class ServerShopLoader : MonoBehaviour, ILoader
    {
        [Inject]
        ShopRegistry shopRegistry;

        public async UniTask Load(WorldData worldData)
        {
            var shops = worldData.worldShopsData.shops;
            shopRegistry.BuildLookup(shops);
        }
    }
}
