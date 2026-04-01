using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Client;
using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.Environment.Structures;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Loaders
{
    public class ClientStructureLoader : ILoader
    {
        [Inject]
        readonly Config config;

        [Inject]
        private readonly Session.Session session;

        [Inject]
        private readonly ModelService modelService;

        [Inject]
        private readonly GltLoaderService gltfLoaderService;

        private readonly GameObject structurePrefab;

        public ClientStructureLoader(ClientPrefabProvider prefabProvider)
        {
            structurePrefab = prefabProvider.StructurePrefab;
        }

        private Dictionary<string, GameObject> modelCache = new();

        public async UniTask Load(string worldId, ZoneData zoneData, CreatablesData creatablesData)
        {
            Dictionary<string, ModelInfo> modelsInfo = await modelService.ListWorldModels(
                worldId,
                GetSessionToken()
            );

            var structures = zoneData.objectPlacementData;
            foreach (StructureData structureData in structures)
            {
                string modelUrl = modelsInfo[structureData.id].url;
                GameObject visual = await GetModel(modelUrl);

                GameObject instance = Object.Instantiate(structurePrefab);
                instance.name = structureData.structureName;
                var controller = instance.GetComponent<StructureController>();

                controller.Initialize(structureData);
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

        private string GetSessionToken()
        {
            return config.IsDebugWorld ? config.ServerAccessToken : session.APIToken;
        }
    }
}
