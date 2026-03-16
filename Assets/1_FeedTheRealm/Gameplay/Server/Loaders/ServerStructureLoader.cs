using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Gameplay.Common.Environment.Structures;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Loaders
{
    public class ServerStructureLoader : MonoBehaviour, ILoader
    {
        private readonly GameObject structurePrefab;

        public ServerStructureLoader(ServerPrefabProvider prefabProvider)
        {
            structurePrefab = prefabProvider.StructureComponent;
        }

        public virtual async UniTask Load(WorldData worldData)
        {
            var structures = worldData.objectPlacementData;
            foreach (StructureData structureData in structures)
            {
                GameObject instance = Instantiate(structurePrefab);
                instance.name = structureData.structureName;
                var controller = instance.GetComponent<StructureController>();
                controller.Initialize(structureData);
            }
        }
    }
}
