using System.Threading.Tasks;
using GLTFast;
using UnityEngine;



namespace API {
    /// <summary>
    /// Service to download item sprites from API.
    /// Handles sprite downloads for items system.
    /// Route: /assets/sprites/items/{spriteId}?category={category}
    /// Separated from AssetsService (character editor sprites).
    /// </summary>
    [CreateAssetMenu(fileName = "GltLoaderService", menuName = "Scriptable Objects/API/GltLoader")]
    public class GltLoaderService : ScriptableObject {
        [Header("Server settings")]
        [SerializeField]
        public string Hostname;

        [SerializeField]
        public int Port;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;
        private string GetBaseUrl() => $"http://{Hostname}:{Port}/assets/models";

        /// <summary>
        ///  Downloads a specific asset model for a given world.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="modelId"></param>
        /// <returns></returns>
        /// <summary>
        /// Downloads and loads a GLB model from the API, instantiates it, and returns the GameObject.
        /// </summary>
        public async Task<GameObject> DownloadAndLoadModel(
            string worldId,
            string modelId
        ) {
            try {
                string url = $"{GetBaseUrl().TrimEnd('/')}/{worldId}/{modelId}";
                logger.Log($"Downloading model from: {url}", this);

                var gltf = new GltfImport();
                bool loaded = await gltf.Load(url);

                if (!loaded) {
                    logger.Log("Failed to download or parse GLB from API", null, Logging.LogType.Error);
                    return null;
                }
                GameObject modelInstance = new(modelId);

                bool instantiated = await gltf.InstantiateMainSceneAsync(modelInstance.transform);

                if (!instantiated) {
                    logger.Log("GLTFast failed to instantiate model.", null, Logging.LogType.Error);
                    return null;
                }

                GameObject instance = modelInstance.transform.GetChild(modelInstance.transform.childCount - 1).gameObject;

                logger.Log($"Model {modelId} loaded and instantiated successfully!", this);
                logger.Log($"Model instance: {instance}", this);
                return modelInstance;
            } catch (System.Exception ex) {
                logger.Log($"Error loading model {modelId}: {ex.Message}", null, Logging.LogType.Error);
                return null;
            }
        }

    }
}
