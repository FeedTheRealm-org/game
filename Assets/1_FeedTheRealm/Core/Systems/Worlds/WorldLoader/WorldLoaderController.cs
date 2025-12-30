using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GLTFast;
using Models;
using API;
using World;
using Worlds;
using System.Threading.Tasks;
using System.IO;
using System;

public class WorldLoaderController : MonoBehaviour {

    [SerializeField] private WorldHandler worldHandler;
    [SerializeField] private GltLoaderService gltLoaderService;
    [SerializeField] private ModelService modelService;
    [SerializeField] private Session.Session session;
    [SerializeField] private WorldController worldController;
    [SerializeField] private Logging.Logger logger;
    [SerializeField] private UIDocument loadingScreenUI;
    [SerializeField] private GameObject enemySpawnPrefab;

    private Dictionary<string, Asset> assetMap;
    private List<GameObject> cleanup = new();

    private async void Start() {

        logger.Log("Loading World", this);
        assetMap = new Dictionary<string, Asset>();
        await LoadAssets();
        LoadWorld();
        if (loadingScreenUI != null) {
            loadingScreenUI.gameObject.SetActive(false);
        }

        // Hide the global loading screen once all world data
        // (models + enemy spawns) has been loaded on the client.
        LoadingScreenEvents.HideWithDelay();
    }

    private async Task LoadAssets() {
        string worldId = worldHandler.selectedWorld.id;

        // Select token according to build type:
        // - Dedicated server (UNITY_SERVER): always environment token.
        // - Client / Editor: always user session token.
        string accessToken;

#if UNITY_SERVER && !UNITY_EDITOR
        accessToken = System.Environment.GetEnvironmentVariable("FTR_SERVER_API_TOKEN");
        if (string.IsNullOrWhiteSpace(accessToken)) {
            logger.Log("[WorldLoaderController] UNITY_SERVER build without FTR_SERVER_API_TOKEN; cannot load world assets.", this, Logging.LogType.Error);
            return;
        }
        logger.Log("[WorldLoaderController] Using server API token from environment for world assets (UNITY_SERVER).", this);
#else
        accessToken = session.APIToken;
        if (string.IsNullOrWhiteSpace(accessToken)) {
            logger.Log("[WorldLoaderController] Session API token is empty; user must be logged in to load world assets.", this, Logging.LogType.Error);
            return;
        }
        logger.Log("[WorldLoaderController] Using player session API token for world assets (client/editor).", this);
#endif

        logger.Log("Fetching model IDs from world...", this);

        // 1. GET MODEL ID LIST
        List<string> modelIds = await modelService.ListWorldAssets(
            worldId,
            accessToken
        );

        if (modelIds.Count == 0) {
            logger.Log("No models found in world", this, Logging.LogType.Warning);
            return;
        }

        // 2. DOWNLOAD & INSTANTIATE MODELS
        foreach (string modelId in modelIds) {
            GameObject modelInstance = await gltLoaderService.DownloadModel(
                worldId,
                modelId
            );
            Asset asset = new(
                modelId,
                modelId,
                new Vector2Int(1, 1),
                modelInstance
            );
            assetMap[modelId] = asset;
            cleanup.Add(modelInstance);
        }
        logger.Log($"Amount of assets loaded: {assetMap.Count}", this);
    }

    public void LoadWorld() {

        WorldData data = worldHandler.selectedWorld.data;

        if (data.objectPlacementData == null || data.objectPlacementData.Count == 0) {
            logger.Log("New world created!", this, Logging.LogType.Info);
            return;
        }

        if (assetMap == null || assetMap.Count == 0) {
            logger.Log("No assets loaded for world; skipping placed object instantiation.", this, Logging.LogType.Warning);
        } else {
            foreach (PlacedAsset placementData in data.objectPlacementData) {
                if (!assetMap.TryGetValue(placementData.AssetDataId, out Asset assetData)) {
                    logger.Log($"Asset with id '{placementData.AssetDataId}' not found in assetMap; skipping placed object.", this, Logging.LogType.Warning);
                    continue;
                }

                Vector3Int gridPosition = placementData.Position;
                worldController.PlaceObjectAt(
                    gridPosition,
                    assetData.GetModelInstance()
                );
            }
        }

        foreach (EnemySpawnAreaData enemySpawnAreaData in data.enemySpawnAreas) {
            worldController.PlaceEnemySpawnAreaAt(
                Vector3Int.FloorToInt(enemySpawnAreaData.Position),
                enemySpawnPrefab
            );
        }
        // since we instantiated models for loading, we need to clean them up
        foreach (GameObject modelInstance in cleanup) {
            Destroy(modelInstance);
        }
        cleanup.Clear();

        logger.Log($"Loaded {data.objectPlacementData.Count} placed objects.", this, Logging.LogType.Info);
    }


}
