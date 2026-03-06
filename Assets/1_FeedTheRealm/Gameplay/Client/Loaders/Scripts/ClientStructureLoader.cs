using System.Collections.Generic;
using System.Runtime.InteropServices;
using API;
using Cysharp.Threading.Tasks;
using FTR.Gameplay.Common.Environment.Structures;
using FTR.Gameplay.Common.WorldLoader.Loaders;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Loaders
{
    public class ClientStructureLoader : StructureLoader
    {
        [Header("Services")]
        [SerializeField]
        private GltLoaderService gltfLoaderService;

        [SerializeField]
        private ModelService modelService;

        [SerializeField]
        private Session.Session session;

        public override async UniTask Load(WorldData worldData)
        {
            await base.Load(worldData);

            Dictionary<string, ModelInfo> modelsInfo = await modelService.ListWorldModels(
                worldData.id,
                session.APIToken
            );

            foreach (GameObject instance in InstanciatedStructures)
            {
                StructureController controller = instance.GetComponent<StructureController>();
                string modelUrl = modelsInfo[controller.Data.id].url;
                GameObject model = await gltfLoaderService.DownloadModel(modelUrl);
                controller.SetVisualModel(model);
            }
        }
    }
}
