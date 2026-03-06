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
        [SerializeField]
        private GltLoaderService gltfLoaderService;

        public override async UniTask Load(WorldData worldData)
        {
            await base.Load(worldData);
            foreach (StructureController controller in structureControllers)
            {
                GameObject model = await gltfLoaderService.DownloadModel(
                    worldData.id,
                    controller.Data.id
                );
                controller.RenderVisual(model);
            }
        }
    }
}
