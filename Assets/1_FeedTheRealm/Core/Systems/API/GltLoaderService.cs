using System.Threading.Tasks;
using API;
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
        public async Task<GameObject> DownloadModel(string worldId, string modelId)
        {
            try
            {
                string url = $"{GetBaseUrl().TrimEnd('/')}/{worldId}/{modelId}";
                GameObject template = new(modelId);
                await GltfHandler.Load(template, url, useLocalAsset: false);
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
