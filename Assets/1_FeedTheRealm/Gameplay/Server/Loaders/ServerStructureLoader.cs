using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Gameplay.Common.Environment.Structures;
using FTR.Gameplay.Server.Characters.Systems;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Loaders
{
    public class ServerStructureLoader : MonoBehaviour, ILoader
    {
        private readonly GameObject structurePrefab;
        private readonly GameObject shopPrefab;

        public ServerStructureLoader(ServerPrefabProvider prefabProvider)
        {
            structurePrefab = prefabProvider.StructureComponent;
            shopPrefab = prefabProvider.ShopComponent;
        }

        public virtual async UniTask Load(
            string worldId,
            ZoneData zoneData,
            CreatablesData creatablesData
        )
        {
            var structureShopData = new List<StructureData>();
            var structures = zoneData.objectPlacementData;
            foreach (StructureData structureData in structures)
            {
                if (structureData.isShop)
                {
                    structureShopData.Add(structureData);
                    continue;
                }
                GameObject instance = Instantiate(structurePrefab);
                instance.name = structureData.structureName;
                var controller = instance.GetComponent<StructureController>();
                controller.Initialize(structureData);
            }

            foreach (StructureData structureData in structureShopData)
            {
                GameObject instance = Instantiate(shopPrefab);
                instance.name = structureData.structureName;
                var controller = instance.GetComponent<StructureController>();
                controller.Initialize(structureData);
            }
        }
    }
}
