using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Gameplay.Common.Environment.Structures;
using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Registry;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Loaders
{
    public class ServerStructureLoader : ILoader
    {
        private readonly GameObject structurePrefab;
        private readonly GameObject shopPrefab;
        private readonly IObjectResolver resolver;

        public ServerStructureLoader(ServerPrefabProvider prefabProvider, IObjectResolver resolver)
        {
            structurePrefab = prefabProvider.StructureComponent;
            shopPrefab = prefabProvider.ShopComponent;
            this.resolver = resolver;
        }

        public virtual async UniTask Load(
            string worldId,
            ZoneData zoneData,
            CreatablesData creatablesData
        )
        {
            ServerShopRegistry.RegisterWorldData(creatablesData);

            var structureShopData = new List<StructureData>();
            var structures = zoneData.objectPlacementData;
            foreach (StructureData structureData in structures)
            {
                if (structureData.isShop)
                {
                    structureShopData.Add(structureData);
                    continue;
                }
                GameObject instance = Object.Instantiate(structurePrefab);
                instance.name = structureData.structureName;
                var controller = instance.GetComponent<StructureController>();
                controller.Initialize(structureData);
            }

            NetworkSpawnPendingObjectsRegistry spawnerRegistry =
                resolver.Resolve<NetworkSpawnPendingObjectsRegistry>();
            foreach (StructureData structureData in structureShopData)
            {
                GameObject instance = Object.Instantiate(shopPrefab);
                instance.name = structureData.shopId;
                var controller = instance.GetComponent<StructureController>();
                controller.Initialize(structureData);
                spawnerRegistry.Register(instance);
            }
        }
    }
}
