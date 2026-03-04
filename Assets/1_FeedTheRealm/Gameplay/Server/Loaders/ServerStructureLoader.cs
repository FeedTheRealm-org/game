using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.Environment.Structures;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Environment.Loaders;

public class ServerStructureLoader : ILoader
{
    private WorldData worldData;
    private GameObject structurePrefab;
    private Logging.Logger logger;

    public ServerStructureLoader(
        WorldData worldData,
        Logging.Logger logger,
        GameObject structurePrefab
    )
    {
        this.worldData = worldData;
        this.structurePrefab = structurePrefab;
        this.logger = logger;
    }

    public async UniTask<WorldData> Load()
    {
        logger.Log(
            "[StructureLoader][Server] Spawning structures | Amount "
                + worldData.objectPlacementData.Count
        );
        foreach (StructureData structureData in worldData.objectPlacementData)
        {
            GameObject instance = UnityEngine.Object.Instantiate(structurePrefab);
            instance.name = structureData.structureName;
            var controller = instance.GetComponent<StructureController>();
            controller.Initialize(structureData);
        }
        logger.Log("[StructureLoader][Server] Finished spawning structures!");

        return worldData;
    }
}
