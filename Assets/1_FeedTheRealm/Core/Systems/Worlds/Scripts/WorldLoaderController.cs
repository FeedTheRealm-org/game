using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using Models;
using UnityEngine;
using UnityEngine.UIElements;

public class WorldLoaderController : MonoBehaviour
{
    [Header("Services")]
    [SerializeField]
    private GltLoaderService gltLoaderService;

    [SerializeField]
    private ModelService modelService;

    [SerializeField]
    private WorldService worldService;

    [SerializeField]
    private Logging.Logger logger;

    [Header("World Object Prefabs")]
    [SerializeField]
    private GameObject enemySpawnPrefab;

    [SerializeField]
    private GameObject shopPrefab;

    [SerializeField]
    private GameObject npcSpawnPrefab;

    [SerializeField]
    private ShopItemsSO shopItemsSO;

    // Private fields
    private string worldId;
    private string accessToken;
    private Dictionary<string, GameObject> modelsMap;

    public async Task<WorldData> LoadWorld(string worldId, string accessToken)
    {
        this.worldId = worldId;
        this.accessToken = accessToken;
        logger.Log("Loading World", this);
        modelsMap = new Dictionary<string, GameObject>();
        await LoadAssets();
        WorldData worldData = await LoadWorldData();
        modelsMap.Clear();
        return worldData;
    }

    private async Task LoadAssets()
    {
        logger.Log("Fetching model IDs from world...", this);
        List<string> modelIds = await modelService.ListWorldAssets(worldId, accessToken);
        if (modelIds.Count == 0)
        {
            logger.Log("No models found in world", this, Logging.LogType.Warning);
            return;
        }
        foreach (string modelId in modelIds)
        {
            GameObject model = await gltLoaderService.DownloadModel(worldId, modelId);
            model.SetActive(false);
            modelsMap[modelId] = model;
        }
        logger.Log($"Amount of assets loaded: {modelsMap.Count}", this);
    }

    private async Task<WorldData> LoadWorldData()
    {
        (WorldData data, string errorMessage, var _) = await worldService.GetWorldData(
            worldId,
            accessToken
        );

        if (data == null || !string.IsNullOrEmpty(errorMessage))
        {
            logger.Log(
                $"[WorldLoaderController] Failed to load world '{worldId}': {errorMessage}",
                this,
                Logging.LogType.Error
            );
            return null;
        }

        logger.Log($"Loading world: {data.worldName}", this);
        shopItemsSO.SetShopData(data.shopData);
        // Register world data so other systems (loot, inventory, tooltips)
        // can query consumables and enemies by spriteId.
        Worlds.WorldItemsRegistry.RegisterWorldData(data);
        foreach (StructureData structureData in data.objectPlacementData)
        {
            if (!modelsMap.TryGetValue(structureData.id, out GameObject modelData))
            {
                logger.Log(
                    $"Model with id '{structureData.id}' not found in modelsMap; skipping placed object.",
                    this,
                    Logging.LogType.Warning
                );
                continue;
            }
            GameObject instance = Instantiate(modelData);
            instance.SetActive(true);
            instance.name = structureData.structureName;
            instance.transform.position = structureData.position;
            instance.transform.rotation = Quaternion.Euler(structureData.rotation);
            instance.transform.localScale = structureData.size;

            if (structureData.isShop)
            {
                GameObject shopInstance = Instantiate(
                    shopPrefab,
                    structureData.position,
                    Quaternion.identity
                );

                shopInstance.GetComponent<BoxCollider>().size = instance
                    .transform.GetChild(0)
                    .GetComponent<BoxCollider>()
                    .size;
                shopInstance.GetComponent<BoxCollider>().center = instance
                    .transform.GetChild(0)
                    .GetComponent<BoxCollider>()
                    .center;
                shopInstance.transform.SetParent(instance.transform);
            }

            logger.Log(
                $"[WorldLoaderController] Placed object '{structureData.structureName}' (assetId={structureData.id}) at {structureData.position}.",
                this
            );
        }

        foreach (EnemySpawnerData enemySpawnAreaData in data.enemySpawnAreas)
        {
            Vector3 targetPosition = enemySpawnAreaData.Position;
            Vector3 spawnPos = targetPosition + new Vector3(0, 0.05f, 0);
            GameObject spawnInstance = Instantiate(enemySpawnPrefab, spawnPos, Quaternion.identity);

            EnemySpawn spawnComponent = spawnInstance.GetComponent<EnemySpawn>();
            if (spawnComponent != null)
            {
                spawnComponent.ConfigureFromSpawnData(enemySpawnAreaData);
            }
            else
            {
                logger.Log(
                    "[WorldLoaderController] Enemy spawn prefab missing EnemySpawn component!",
                    this,
                    Logging.LogType.Error
                );
            }

            spawnInstance.SetActive(true);
            logger.Log(
                $"[WorldLoaderController] Placed enemy spawn area at {spawnPos} with radius {enemySpawnAreaData.Radius}.",
                this
            );
        }

        foreach (NPCSpawnerData npcSpawnAreaData in data.npcSpawnAreas)
        {
            Vector3 targetPosition = npcSpawnAreaData.Position;
            Vector3 spawnPos = targetPosition + new Vector3(0, 0.05f, 0);
            GameObject spawnInstance = Instantiate(npcSpawnPrefab, spawnPos, Quaternion.identity);
            NPCSpawns spawnComponent = spawnInstance.GetComponent<NPCSpawns>();
            if (spawnComponent != null)
            {
                spawnComponent.ConfigureFromSpawnData(npcSpawnAreaData, data.dialogs[0]);
            }
            else
            {
                logger.Log(
                    "[WorldLoaderController] NPC spawn prefab missing NPCSpawns component!",
                    this,
                    Logging.LogType.Error
                );
            }

            spawnInstance.SetActive(true);
            logger.Log(
                $"[WorldLoaderController] Placed NPC spawn area at {spawnPos} with radius {npcSpawnAreaData.Radius}.",
                this
            );
        }

        logger.Log(
            $"Loaded {data.objectPlacementData.Count} placed objects.",
            this,
            Logging.LogType.Info
        );
        return data;
    }
}
