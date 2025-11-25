using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Loads and builds the world from WorldLoader data when the GameScene starts.
/// Attach this to a GameObject in the GameScene.
/// </summary>
public class WorldBuilder : MonoBehaviour {
    [Header("References")]
    [SerializeField]
    private Systems.WorldLoader worldLoader;

    [SerializeField]
    private World.WorldController worldController;

    [Header("Settings")]
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private bool loadOnStart = true;

    [Header("Asset ID Mapping")]
    [Tooltip("Map asset IDs to folder names. Example: ID '4' maps to folder 'Building'")]
    [SerializeField]
    private AssetIdMapping[] assetIdMappings;

    [System.Serializable]
    public class AssetIdMapping {
        public string assetId;
        public string folderName;
    }

    private Dictionary<string, Models.Asset> assetLookup;

    private void Awake() {
        assetLookup = new Dictionary<string, Models.Asset>();
    }

    private void Start() {
        if (loadOnStart) {
            StartCoroutine(LoadAndBuildWorld());
        }
    }

    /// <summary>
    /// Load assets from API and build the world
    /// </summary>
    private System.Collections.IEnumerator LoadAndBuildWorld() {
        logger?.Log("========== LOADING WORLD ==========", this);

        if (worldLoader == null) {
            logger?.Log("WorldLoader is not assigned!", this, Logging.LogType.Error);
            yield break;
        }

        if (worldController == null) {
            logger?.Log("WorldController is not assigned!", this, Logging.LogType.Error);
            yield break;
        }

        // Get world data
        Models.WorldData worldData = worldLoader.GetWorldData();
        if (worldData == null) {
            logger?.Log("No world data available to build", this, Logging.LogType.Warning);
            yield break;
        }

        logger?.Log($"World: {worldData.worldName}", this);
        logger?.Log($"Assets to place: {worldData.objectPlacementData?.Count ?? 0}", this);

        // Get unique asset IDs from world data
        HashSet<string> requiredAssetIds = new HashSet<string>();
        if (worldData.objectPlacementData != null) {
            foreach (var placedAsset in worldData.objectPlacementData) {
                requiredAssetIds.Add(placedAsset.AssetDataId);
            }
        }

        if (requiredAssetIds.Count == 0) {
            logger?.Log("No assets required for this world", this, Logging.LogType.Info);
            yield break;
        }

        // Load assets from Resources folder (downloaded models)
        logger?.Log($"Loading {requiredAssetIds.Count} assets from Resources...", this);
        string worldId = worldLoader.worldId;

        foreach (string assetId in requiredAssetIds) {
            logger?.Log($"Attempting to load asset ID: {assetId}", this);
            var asset = LoadAssetFromResources(worldId, assetId);
            if (asset != null) {
                assetLookup[assetId] = asset;
                logger?.Log($"✅ Successfully added asset {assetId} to lookup", this);
            } else {
                logger?.Log($"❌ Failed to load asset {assetId}", this, Logging.LogType.Warning);
            }
        }

        logger?.Log($"Loaded {assetLookup.Count}/{requiredAssetIds.Count} assets", this);

        // Now build the world
        BuildWorld();
        yield break;
    }

    /// <summary>
    /// Build the world from WorldLoader data
    /// </summary>
    public void BuildWorld() {
        logger?.Log("========== BUILDING WORLD ==========", this);

        Models.WorldData worldData = worldLoader.GetWorldData();
        if (worldData == null) {
            logger?.Log("No world data available", this, Logging.LogType.Warning);
            return;
        }

        // Place all objects
        if (worldData.objectPlacementData != null && worldData.objectPlacementData.Count > 0) {
            int placedCount = 0;
            int errorCount = 0;

            foreach (var placedAsset in worldData.objectPlacementData) {
                logger?.Log($"Attempting to place asset ID: {placedAsset.AssetDataId} at position: {placedAsset.Position}", this);
                if (PlaceAsset(placedAsset)) {
                    placedCount++;
                } else {
                    errorCount++;
                }
            }

            logger?.Log($"✅ Placed {placedCount} objects successfully", this);
            if (errorCount > 0) {
                logger?.Log($"⚠️ Failed to place {errorCount} objects", this, Logging.LogType.Warning);
            }
        } else {
            logger?.Log("No objects to place in this world", this, Logging.LogType.Info);
        }

        logger?.Log("========== WORLD BUILD COMPLETE ==========", this);
    }

    /// <summary>
    /// Place a single asset in the world
    /// </summary>
    private bool PlaceAsset(Models.PlacedAsset placedAsset) {
        if (placedAsset == null) {
            logger?.Log("PlacedAsset is null", this, Logging.LogType.Warning);
            return false;
        }

        // Find the asset definition
        if (!assetLookup.TryGetValue(placedAsset.AssetDataId, out Models.Asset assetData)) {
            logger?.Log($"⚠️ Asset {placedAsset.AssetDataId} was not loaded. Check if the model exists in Resources/WorldModels/{worldLoader.worldId}/", this, Logging.LogType.Warning);
            return false;
        }

        // Instantiate the model
        GameObject instance = assetData.AssetModelInstance;
        if (instance == null) {
            logger?.Log($"Failed to instantiate model for asset: {assetData.Name} (ID: {assetData.Id})", this, Logging.LogType.Error);
            return false;
        }

        // Place the object at the grid position
        worldController.PlaceObjectAt(placedAsset.Position, instance);

        // Log the world position
        Vector3 worldPos = worldController.GetCellPosition(placedAsset.Position);
        logger?.Log($"Placed at world position: {worldPos}", this);

        // Store reference in placedAsset for future use
        placedAsset.InstancedGameObject = instance;

        logger?.Log($"Placed asset: {assetData.Name} at {placedAsset.Position}", this);
        return true;
    }

    /// <summary>
    /// Clear all placed objects from the world
    /// </summary>
    public void ClearWorld() {
        if (worldLoader == null) return;

        Models.WorldData worldData = worldLoader.GetWorldData();
        if (worldData?.objectPlacementData == null) return;

        foreach (var placedAsset in worldData.objectPlacementData) {
            if (placedAsset.InstancedGameObject != null) {
                worldController.RemoveObject(placedAsset.InstancedGameObject);
                placedAsset.InstancedGameObject = null;
            }
        }

        logger?.Log("World cleared", this);
    }

    private void OnDestroy() {
        // Optional: Clear world data when leaving the scene
        // worldLoader?.Clear();
    }

    /// <summary>
    /// Get all unique asset IDs from the world data for debugging
    /// </summary>
    private HashSet<string> GetUniqueAssetIds() {
        HashSet<string> ids = new HashSet<string>();
        Models.WorldData worldData = worldLoader?.GetWorldData();
        if (worldData?.objectPlacementData != null) {
            foreach (var placedAsset in worldData.objectPlacementData) {
                ids.Add(placedAsset.AssetDataId);
            }
        }
        return ids;
    }

    /// <summary>
    /// Load an asset from Resources folder based on world ID and asset ID
    /// </summary>
    private Models.Asset LoadAssetFromResources(string worldId, string assetId) {
        // Check if there's a mapping for this asset ID
        string folderName = assetId;
        if (assetIdMappings != null) {
            foreach (var mapping in assetIdMappings) {
                if (mapping.assetId == assetId) {
                    folderName = mapping.folderName;
                    logger?.Log($"Mapped asset ID {assetId} to folder '{folderName}'", this);
                    break;
                }
            }
        }

        // Try to load from downloaded world models
        string basePath = $"WorldModels/{worldId}/{folderName}/model";
        GameObject prefab = Resources.Load<GameObject>(basePath);

        if (prefab == null) {
            // Try alternative paths
            basePath = $"WorldModels/{worldId}/{folderName}";
            prefab = Resources.Load<GameObject>(basePath);
        }

        if (prefab == null) {
            // Try with asset_ prefix
            basePath = $"WorldModels/{worldId}/asset_{assetId}";
            prefab = Resources.Load<GameObject>(basePath);
        }

        if (prefab == null) {
            // Try folderName/folderName
            basePath = $"WorldModels/{worldId}/{folderName}/{folderName}";
            prefab = Resources.Load<GameObject>(basePath);
        }

        if (prefab == null) {
            logger?.Log($"⚠️ Asset {assetId} (folder: {folderName}) not found in Resources at WorldModels/{worldId}/", this, Logging.LogType.Warning);
            logger?.Log($"   Tried paths: WorldModels/{worldId}/{folderName}/model, WorldModels/{worldId}/{folderName}, WorldModels/{worldId}/asset_{assetId}, WorldModels/{worldId}/{folderName}/{folderName}", this, Logging.LogType.Info);
            return null;
        }

        // Create asset definition
        var asset = System.Activator.CreateInstance(typeof(Models.Asset), true) as Models.Asset;

        var idField = typeof(Models.Asset).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nameField = typeof(Models.Asset).GetField("name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var sizeField = typeof(Models.Asset).GetField("size", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var modelPathField = typeof(Models.Asset).GetField("modelPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var materialPathField = typeof(Models.Asset).GetField("materialPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        idField?.SetValue(asset, assetId);
        nameField?.SetValue(asset, prefab.name);
        sizeField?.SetValue(asset, Vector2Int.one); // Default size
        modelPathField?.SetValue(asset, basePath);
        materialPathField?.SetValue(asset, $"WorldModels/{worldId}/{folderName}/material");

        logger?.Log($"✅ Loaded asset {assetId} from {basePath}", this);
        return asset;
    }
}
