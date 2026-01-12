using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace API
{
    /// <summary>
    /// Service to download item sprites from API.
    /// Handles sprite downloads for items system.
    /// Route: /assets/sprites/items/{spriteId}?category={category}
    /// Separated from AssetsService (character editor sprites).
    /// </summary>
    [CreateAssetMenu(fileName = "GltLoaderService", menuName = "Scriptable Objects/API/GltLoader")]
    public class GltLoaderService : ScriptableObject
    {
        [Header("API Config")]
        [SerializeField]
        private ApiConfig apiConfig;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        private string GetBaseUrl() =>
            $"http://{apiConfig.Hostname}:{apiConfig.Port}/assets/models";

        /// <summary>
        ///  Downloads a specific asset model for a given world.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="modelId"></param>
        /// <returns></returns>
        /// <summary>
        /// Downloads and loads a GLB model from the API, instantiates it, and returns the GameObject.
        /// </summary>
        public async Task<GameObject> DownloadModel(string worldId, string modelId)
        {
            try
            {
                string url = $"{GetBaseUrl().TrimEnd('/')}/{worldId}/{modelId}";

                var gltf = new GltfImport();
                bool loaded = await gltf.Load(url);
                if (!loaded)
                    return null;

                GameObject template = new(modelId);

                await gltf.InstantiateMainSceneAsync(template.transform);

                // Normalize GLTF root (this depends on how the GLTF was created/exported)
                Transform gltfRoot = template.transform.GetChild(0);
                gltfRoot.localPosition = Vector3.zero;
                gltfRoot.localScale = Vector3.one;

                return template;
            }
            catch (System.Exception ex)
            {
                logger.Log(
                    $"Error loading model {modelId}: {ex.Message}",
                    null,
                    Logging.LogType.Error
                );
                return null;
            }
        }
    }
}
