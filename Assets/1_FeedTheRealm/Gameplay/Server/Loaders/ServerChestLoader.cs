using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Gameplay.Common.Environment.Chests;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Chest;
using FTR.Gameplay.Server.Environment.Chest;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Loaders
{
    /// <summary>
    /// Loader that populates ChestRegistry with portal data.
    /// </summary>
    public class ServerChestLoader : ILoader
    {
        private GameObject chestPrefab;
        private IObjectResolver resolver;

        public ServerChestLoader(ServerPrefabProvider prefabProvider, IObjectResolver resolver)
        {
            chestPrefab = prefabProvider.ChestPrefab;
            this.resolver = resolver;
        }

        public async UniTask Load(string world_id, ZoneData zoneData, CreatablesData creatablesData)
        {
            foreach (var chestData in zoneData.chestPlacements)
            {
                GameObject instance = resolver.Instantiate(chestPrefab);
                instance.name = $"Chest_{chestData.id}";
                instance.GetComponent<ChestController>().Initialize(chestData);
                instance.GetComponent<GameObjectLinker>().Initialize();
            }
            await UniTask.CompletedTask;
        }
    }
}
