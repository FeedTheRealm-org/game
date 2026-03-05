using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.Environment.Structures;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.WorldLoader.Loaders
{
    public class ServerStructureLoader : MonoBehaviour, ILoader
    {
        [SerializeField]
        private GameObject structurePrefab;

        public async UniTask Load(WorldData worldData)
        {
            foreach (StructureData structureData in worldData.objectPlacementData)
            {
                GameObject instance = Instantiate(structurePrefab);
                instance.name = structureData.structureName;
                var controller = instance.GetComponent<StructureController>();
                controller.Initialize(structureData);
            }
        }
    }
}
