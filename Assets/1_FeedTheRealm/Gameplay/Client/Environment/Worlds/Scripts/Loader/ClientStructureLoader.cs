using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.Environment.Structures;
using Mirror;
using Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Environment.Worlds.Loader;

public class ClientStructureLoader : ILoader
{
    private WorldData worldData;
    private GltLoaderService gltLoaderService;
    private ModelService modelService;
    private GameObject structurePrefab;
    private Logging.Logger logger;

    public ClientStructureLoader(
        GltLoaderService gltLoaderService,
        ModelService modelService,
        GameObject structurePrefab,
        Logging.Logger logger
    )
    {
        this.gltLoaderService = gltLoaderService;
        this.modelService = modelService;
        this.structurePrefab = structurePrefab;
        this.logger = logger;
    }

    private readonly Dictionary<string, GameObject> modelsMap = new();

    public async UniTask<WorldData> Load()
    {
        logger.Log("[StructureLoader][Client] Loading structure visuals");

        Dictionary<string, GameObject> cachedModels = new();

        foreach (StructureData structureData in worldData.objectPlacementData)
        {
            GameObject referenceModel;
            if (cachedModels.TryGetValue(structureData.id, out GameObject cachedModel))
            {
                referenceModel = cachedModel;
            }
            else
            {
                referenceModel = await gltLoaderService.DownloadModel(
                    worldData.id,
                    structureData.id
                );
                referenceModel.SetActive(false);
                cachedModels[structureData.id] = referenceModel;
            }

            GameObject structure = UnityEngine.Object.Instantiate(structurePrefab);
            var controller = structure.GetComponent<StructureController>();
            controller.Initialize(structureData);
            controller.RenderVisual(referenceModel);
        }

        logger.Log("[StructureLoader][Client] Structure visuals loaded");
        return worldData;
    }
}
