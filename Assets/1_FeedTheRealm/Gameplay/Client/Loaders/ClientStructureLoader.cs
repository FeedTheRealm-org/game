using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FeedTheRealm.Core.EventChannels.Setup;
using FTR.Core.Client;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.Environment.Structures;
using FTRShared.Runtime.Core.Cache;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Loaders
{
    public class ClientStructureLoader : ILoader
    {
        private LoadingProgressEvent loadingProgressEvent;
        private readonly ModelService modelService;
        private readonly CacheManager cacheManager;
        private readonly GameObject structurePrefab;
        private readonly GameObject shopPrefab;
        private readonly GameObject goldCoinPrefab;
        private readonly ColliderRegistry colliderRegistry;

        public ClientStructureLoader(
            ClientPrefabProvider prefabProvider,
            ColliderRegistry colliderRegistry,
            ModelService modelService,
            CacheManager cacheManager
        )
        {
            this.modelService = modelService;
            this.cacheManager = cacheManager;
            this.colliderRegistry = colliderRegistry;
            structurePrefab = prefabProvider.StructurePrefab;
            shopPrefab = prefabProvider.ShopPrefab;
            goldCoinPrefab = prefabProvider.GoldCoinPrefab;
        }

        private Dictionary<string, GameObject> modelCache = new();

        public async UniTask Load(string worldId, ZoneData zoneData, CreatablesData creatablesData)
        {
            Dictionary<string, ModelInfo> modelsInfo = await modelService.ListWorldModels(worldId);

            ClientShopRegistry.RegisterWorldData(creatablesData);

            int totalStructures = zoneData.objectPlacementData.Count;
            int structuresProcessed = 0;

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
                string updatedAt = modelsInfo[structureData.id].updated_at;
                GameObject visual = await GetModel(modelUrl, updatedAt);

                GameObject instance = UnityEngine.Object.Instantiate(structurePrefab);
                instance.name = structureData.structureName;
                var controller = instance.GetComponent<StructureController>();
                var (collider, colliderLayer) = colliderRegistry.GetCollider(
                    structureData.colliderType
                );
                controller.Initialize(structureData, collider, colliderLayer);
                controller.SetupMesh(visual);

                structuresProcessed++;
                if (structuresProcessed % Mathf.Max(1, totalStructures / 10) == 0)
                {
                    loadingProgressEvent?.Raise(
                        (float)structuresProcessed / totalStructures * 0.8f
                    );
                }
            }

            foreach (StructureData shopData in shopStructures)
            {
                string modelUrl = modelsInfo[shopData.id].url;
                string updatedAt = modelsInfo[shopData.id].updated_at;
                GameObject visual = await GetModel(modelUrl, updatedAt);

                GameObject instance = UnityEngine.Object.Instantiate(shopPrefab);
                instance.name = shopData.structureName;
                var controller = instance.GetComponent<StructureController>();
                var (collider, colliderLayer) = colliderRegistry.GetCollider(shopData.colliderType);
                controller.Initialize(shopData, collider, colliderLayer);
                controller.SetupMesh(visual);

                var goldCoin = Object.Instantiate(goldCoinPrefab, instance.transform);
                goldCoin.SetActive(true);

                setUpVFX(instance, goldCoin.transform);

                structuresProcessed++;
                if (structuresProcessed % Mathf.Max(1, totalStructures / 10) == 0)
                {
                    loadingProgressEvent?.Raise(
                        (float)structuresProcessed / totalStructures * 0.8f
                    );
                }
            }

            loadingProgressEvent?.Raise(0.8f);

            modelCache.Clear();
            modelsInfo.Clear();
        }

        private async UniTask<GameObject> GetModel(string modelUrl, string updatedAt)
        {
            if (modelCache.ContainsKey(modelUrl))
                return modelCache[modelUrl];

            GameObject visual = null;
            try
            {
                var timeStamp = DateTimeHelper.ParseDateTimeOffset(updatedAt);
                visual = await cacheManager.GetModel(modelUrl, timeStamp);
            }
            catch
            {
                Debug.LogError($"Failed to load model: {modelUrl}.");
            }

            visual.SetActive(false);
            modelCache[modelUrl] = visual;
            return visual;
        }

        private static void setUpVFX(GameObject root, Transform vfx)
        {
            var controller = root.GetComponent<StructureController>();
            if (controller == null)
            {
                Debug.LogWarning("[ClientStructureLoader] StructureController not found.");
                return;
            }

            if (controller.Data == null)
            {
                Debug.LogWarning("[ClientStructureLoader] StructureData is null.");
                return;
            }

            var data = controller.Data;
            float topY =
                root.transform.position.y + data.colliderCenter.y + (data.colliderSize.y * 0.5f);

            float yOffset = 0.5f;
            vfx.position = new Vector3(
                root.transform.position.x + data.colliderCenter.x,
                topY + yOffset,
                root.transform.position.z + data.colliderCenter.z
            );

            vfx.transform.localScale = new Vector3(2, 2, 2);
        }
    }
}
