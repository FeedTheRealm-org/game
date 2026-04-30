using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Gameplay.Common.Environment.Chests;
using FTR.Gameplay.Common.NetworkEntities.Chest;
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
            NetworkSpawnPendingObjectsRegistry spawnerRegistry =
                resolver.Resolve<NetworkSpawnPendingObjectsRegistry>();

            foreach (var chestData in zoneData.chestPlacements)
            {
                GameObject instance = resolver.Instantiate(chestPrefab);
                var chestStateStorage = instance.GetComponent<ChestStateStorage>();
                chestStateStorage.SetChestData(chestData);
                Debug.Log(
                    $"[ServerChestLoader] Loaded chest with ID {chestData.id} at position {chestData.position}. Instance: {instance != null}"
                );
                instance.GetComponent<ChestController>().Initialize(chestStateStorage);
                spawnerRegistry.Register(instance);
            }
            await UniTask.CompletedTask;
        }
    }
}
