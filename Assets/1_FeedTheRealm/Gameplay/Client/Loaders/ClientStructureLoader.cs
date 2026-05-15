using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Client;
using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.Environment.Structures;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Loaders
{
    public class ClientStructureLoader : ILoader
    {
        private readonly ModelService modelService;
        private readonly GltLoaderService gltfLoaderService;
        private readonly GameObject structurePrefab;
        private readonly GameObject shopPrefab;
        private readonly ColliderRegistry colliderRegistry;

        public ClientStructureLoader(
            ClientPrefabProvider prefabProvider,
            ColliderRegistry colliderRegistry,
            ModelService modelService,
            GltLoaderService gltfLoaderService
        )
        {
            this.modelService = modelService;
            this.gltfLoaderService = gltfLoaderService;
            this.colliderRegistry = colliderRegistry;
            structurePrefab = prefabProvider.StructurePrefab;
            shopPrefab = prefabProvider.ShopPrefab;
        }

        private Dictionary<string, GameObject> modelCache = new();

        public async UniTask Load(string worldId, ZoneData zoneData, CreatablesData creatablesData)
        {
            Dictionary<string, ModelInfo> modelsInfo = await modelService.ListWorldModels(worldId);

            ClientShopRegistry.RegisterWorldData(creatablesData);

            var structures = zoneData.objectPlacementData;
            var shopStructures = new List<StructureData>();
            foreach (StructureData structureData in structures)
            {
                if (structureData.isShop)
                {
                    shopStructures.Add(structureData);
                    continue;
                }
                string modelUrl = modelsInfo[structureData.id].url;
                GameObject visual = await GetModel(modelUrl);

                GameObject instance = Object.Instantiate(structurePrefab);
                instance.name = structureData.structureName;
                var controller = instance.GetComponent<StructureController>();
                var (collider, colliderLayer) = colliderRegistry.GetCollider(
                    structureData.colliderType
                );
                controller.Initialize(structureData, collider, colliderLayer);
                controller.SetupMesh(visual);
            }

            foreach (StructureData shopData in shopStructures)
            {
                string modelUrl = modelsInfo[shopData.id].url;
                GameObject visual = await GetModel(modelUrl);

                GameObject instance = Object.Instantiate(shopPrefab);
                instance.name = shopData.structureName;
                var controller = instance.GetComponent<StructureController>();
                var (collider, colliderLayer) = colliderRegistry.GetCollider(shopData.colliderType);
                controller.Initialize(shopData, collider, colliderLayer);
                controller.SetupMesh(visual);
            }

            modelCache.Clear();
            modelsInfo.Clear();
        }

        private async UniTask<GameObject> GetModel(string modelUrl)
        {
            if (modelCache.ContainsKey(modelUrl))
                return modelCache[modelUrl];

            GameObject visual = await gltfLoaderService.DownloadModel(modelUrl);
            visual.SetActive(false);
            modelCache[modelUrl] = visual;
            return visual;
        }
    }
}
