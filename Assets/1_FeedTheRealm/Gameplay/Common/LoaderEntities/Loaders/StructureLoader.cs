using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.Environment.Structures;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Common.WorldLoader.Loaders
{
    public class StructureLoader : MonoBehaviour, ILoader
    {
        [SerializeField]
        private Config config;

        [SerializeField]
        private ModelService modelService;

        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private GltLoaderService gltfLoaderService;

        [SerializeField]
        private GameObject structurePrefab;

        private Dictionary<string, GameObject> modelCache = new();

        public async UniTask Load(WorldData worldData)
        {
            Dictionary<string, ModelInfo> modelsInfo = await modelService.ListWorldModels(
                worldData.id,
                session.APIToken
            );

            var structures = worldData.objectPlacementData;
            foreach (StructureData structureData in structures)
            {
                string modelUrl = modelsInfo[structureData.id].url;
                GameObject visual = await GetModel(modelUrl);

                GameObject instance = Instantiate(structurePrefab);
                instance.name = structureData.structureName;
                var controller = instance.GetComponent<StructureController>();

                controller.Initialize(structureData, visual);

                if (config.RuntimeRole == RuntimeRole.Server)
                    controller.RemoveVisual();
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
