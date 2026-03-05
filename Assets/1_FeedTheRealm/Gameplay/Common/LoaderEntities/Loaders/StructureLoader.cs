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
        private GameObject structurePrefab;

        public async UniTask Load(WorldData worldData)
        {
            var structures = worldData.objectPlacementData;
            foreach (StructureData structureData in structures)
            {
                GameObject instance = Instantiate(structurePrefab);
                instance.name = structureData.structureName;
                var controller = instance.GetComponent<StructureController>();
                controller.Initialize(structureData);

                RenderVisual(controller);
            }
        }

        public virtual void RenderVisual(StructureController controller) { }
    }
}
