using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using Mirror;
using Models;
using UnityEngine;

namespace Core.Systems.Worlds.Loader
{
    public class StructureLoaderController : MonoBehaviour, IServerLoader, IClientLoader
    {
        [Header("Logger")]
        [SerializeField]
        private Logging.Logger logger;

        [Header("Services")]
        [SerializeField]
        private GltLoaderService gltLoaderService;

        [SerializeField]
        private ModelService modelService;

        [SerializeField]
        private GameObject structurePrefab;

        private Dictionary<string, GameObject> modelsMap = new();

        public async Task LoadServer(WorldData worldData, string accessToken)
        {
            logger.Log(
                "[StructureLoader][Server] Spawning structures | Amount "
                    + worldData.objectPlacementData.Count,
                this
            );
            foreach (StructureData data in worldData.objectPlacementData)
            {
                GameObject instance = Instantiate(structurePrefab);
                instance.name = data.structureName;
                var controller = instance.GetComponent<StructureController>();
                controller.Initialize(data);
                NetworkServer.Spawn(instance);
                logger.Log($"[Server] Spawned structure '{data.structureName}'", this);
            }
            logger.Log("[StructureLoader][Server] Finished spawning structures!", this);
        }

        public async Task LoadClient(WorldData worldData, string accessToken)
        {
            logger.Log("[StructureLoader][Client] Loading structure visuals", this);
            await LoadStrucutreModels(worldData.id, accessToken);
            RenderStructures();
        }

        private async Task LoadStrucutreModels(string worldId, string accessToken)
        {
            logger.Log("[Client] Fetching model IDs...", this);

            List<string> modelIds = await modelService.ListWorldAssets(worldId, accessToken);
            if (modelIds.Count == 0)
            {
                logger.Log("[Client] No models found", this, Logging.LogType.Warning);
                return;
            }

            foreach (string modelId in modelIds)
            {
                GameObject model = Instantiate(structurePrefab);
                model = await gltLoaderService.DownloadModel(worldId, modelId, model);
                model.SetActive(false);
                modelsMap[modelId] = model;
            }
        }

        private void RenderStructures()
        {
            // TODO: consider a better approach than FindObjectsOfType
            foreach (
                StructureController controller in FindObjectsByType<StructureController>(
                    FindObjectsSortMode.None
                )
            )
            {
                if (!modelsMap.TryGetValue(controller.ModelId, out GameObject model))
                    continue;

                controller.RenderVisual(model);
            }
        }
    }
}
