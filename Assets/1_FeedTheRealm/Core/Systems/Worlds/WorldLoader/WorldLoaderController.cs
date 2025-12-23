using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using API;
using GLTFast;
using Models;
using UnityEngine;
using UnityEngine.UIElements;
using World;
using Worlds;

public class WorldLoaderController : MonoBehaviour
{
    [SerializeField]
    private WorldHandler worldHandler;

    [SerializeField]
    private GltLoaderService gltLoaderService;

    [SerializeField]
    private ModelService modelService;

    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private WorldController worldController;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private UIDocument loadingScreenUI;

    [SerializeField]
    private GameObject enemySpawnPrefab;

    private Dictionary<string, Asset> assetMap;
    private List<GameObject> cleanup = new();

    private async void Start()
    {
        logger.Log("Loading World", this);
        assetMap = new Dictionary<string, Asset>();
        await LoadAssets();
        LoadWorld();
        loadingScreenUI.gameObject.SetActive(false);
    }

    private async Task LoadAssets()
    {
        string worldId = worldHandler.selectedWorld.id;
        logger.Log("Fetching model IDs from world...", this);

        // 1. GET MODEL ID LIST
        List<string> modelIds = await modelService.ListWorldAssets(worldId, session.APIToken);

        if (modelIds.Count == 0)
        {
            logger.Log("No models found in world", this, Logging.LogType.Warning);
            return;
        }

        // 2. DOWNLOAD & INSTANTIATE MODELS
        foreach (string modelId in modelIds)
        {
            GameObject modelInstance = await gltLoaderService.DownloadModel(worldId, modelId);
            Asset asset = new(modelId, modelId, new Vector2Int(1, 1), modelInstance);
            assetMap[modelId] = asset;
            cleanup.Add(modelInstance);
        }
        logger.Log($"Amount of assets loaded: {assetMap.Count}", this);
    }

    public void LoadWorld()
    {
        WorldData data = worldHandler.selectedWorld.data;

        if (data.objectPlacementData == null || data.objectPlacementData.Count == 0)
        {
            logger.Log("New world created!", this, Logging.LogType.Info);
            return;
        }

        foreach (PlacedAsset placementData in data.objectPlacementData)
        {
            Asset assetData = assetMap[placementData.AssetDataId];
            Vector3Int gridPosition = placementData.Position;
            worldController.PlaceObjectAt(gridPosition, assetData.GetModelInstance());
        }

        foreach (EnemySpawnAreaData enemySpawnAreaData in data.enemySpawnAreas)
        {
            worldController.PlaceEnemySpawnAreaAt(
                Vector3Int.FloorToInt(enemySpawnAreaData.Position),
                enemySpawnPrefab
            );
        }
        // since we instantiated models for loading, we need to clean them up
        foreach (GameObject modelInstance in cleanup)
        {
            Destroy(modelInstance);
        }
        cleanup.Clear();

        logger.Log(
            $"Loaded {data.objectPlacementData.Count} placed objects.",
            this,
            Logging.LogType.Info
        );
    }
}
