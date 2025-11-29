using System.Collections;
using System.Collections.Generic;
using API;
using Models;
using UnityEngine;
using UnityEngine.UIElements;
using World;
using Worlds;
using System.IO;

public class WorldLoaderController : MonoBehaviour {

    [Header("World Loader Settings")]
    [SerializeField] private WorldHandler worldHandler;
    [SerializeField] private ModelService modelService;
    [SerializeField] private Session.Session session;
    [SerializeField] private WorldController worldController;
    [SerializeField] private Logging.Logger logger;
    [SerializeField] private string modelsResourcePath = "Resources";
    [SerializeField] private UIDocument loadingScreenUI;

    private Dictionary<string, Asset> assetMap;

    private void Awake() {
        assetMap = new Dictionary<string, Asset>();
        ShowLoadingScreen(true);
        StartCoroutine(LoadEverything());
    }

    private IEnumerator LoadEverything() {
        yield return StartCoroutine(DownloadModlesCoroutine());
        LoadModels();
        LoadWorld();
        ShowLoadingScreen(false);
    }

    private void ShowLoadingScreen(bool show) {
        if (loadingScreenUI != null) {
            loadingScreenUI.rootVisualElement.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private IEnumerator DownloadModlesCoroutine() {
        var selectedWorld = worldHandler.GetSelectedWorld();
        var token = session?.APIToken;

        if (selectedWorld == null) {
            logger.Log("No world selected to download models for.", this, Logging.LogType.Warning);
            yield break;
        }

        if (string.IsNullOrEmpty(token)) {
            logger.Log("No API token available (Session is null or token empty).", this, Logging.LogType.Error);
            yield break;
        }

        string worldId = selectedWorld.id;
        string destination = Path.Combine(Application.dataPath, modelsResourcePath, worldId);

        bool downloadComplete = false;

        StartCoroutine(modelService.DownloadAndExtractAssets(worldId, token, destination, (error) => {
            if (!string.IsNullOrEmpty(error)) {
                logger.Log($"Model download/extract failed: {error}", this, Logging.LogType.Error);
            } else {
                logger.Log($"Models downloaded and extracted to: {destination}", this, Logging.LogType.Info);
            }
            downloadComplete = true;
        }));

        // Wait until download is complete
        while (!downloadComplete) {
            yield return null;
        }
    }

    public void LoadModels() {
        var selectedWorld = worldHandler.GetSelectedWorld();
        if (selectedWorld == null) {
            logger.Log("No world selected to load models for.", this, Logging.LogType.Warning);
            return;
        }

        string worldId = selectedWorld.id;
        string modelsPath = worldId;

        assetMap.Clear();

        // Get the actual folder structure from the file system
        string fullPath = Path.Combine(Application.dataPath, modelsResourcePath, worldId);
        if (!Directory.Exists(fullPath)) {
            logger.Log($"Models directory not found: {fullPath}", this, Logging.LogType.Error);
            return;
        }

        string[] assetFolderPaths = Directory.GetDirectories(fullPath);

        foreach (string assetFolderPath in assetFolderPaths) {
            string assetId = Path.GetFileName(assetFolderPath);
            string modelPath = Path.Combine(modelsPath, assetId, "model");
            string materialPath = Path.Combine(modelsPath, assetId, "material");

            Asset asset = new(
                assetId,
                assetId,
                new Vector2Int(1, 1),
                modelPath,
                materialPath
            );

            assetMap[assetId] = asset;
            logger.Log($"Created asset {assetId} with modelPath: {modelPath}", this);
        }

        logger.Log($"Loaded {assetMap.Count} assets for world {worldId}", this);
    }

    public void LoadWorld() {
        WorldData selectedWorld = worldHandler.GetSelectedWorld();
        logger.Log(selectedWorld, this);
        if (selectedWorld == null) {
            logger.Log("No world selected to load.", this, Logging.LogType.Warning);
            return;
        }
        foreach (PlacedAsset placementData in selectedWorld.objectPlacementData) {
            Asset assetData = assetMap[placementData.AssetDataId];
            Vector3Int gridPosition = placementData.Position;
            worldController.PlaceObjectAt(gridPosition, assetData.AssetModelInstance);
        }

        logger.Log($"Loaded {selectedWorld.objectPlacementData.Count} placed objects.", this, Logging.LogType.Info);
    }
}
