using System.Threading.Tasks;
using UnityEngine;

namespace API
{
    /// <summary>
    /// Service to download item sprites from API.
    /// Handles sprite downloads for items system.
    /// Route: /assets/sprites/items/{spriteId}?category={category}
    /// Separated from AssetsService (character editor sprites).
    /// </summary>
    [CreateAssetMenu(
        fileName = "GltLoaderService",
        menuName = "Scriptable Objects/API/GltLoaderService"
    )]
    public class GltLoaderService : ScriptableObject
    {
        [SerializeField]
        private ApiConfig apiConfig;
        private GltfHandler gltfHandler = new();

        public GltLoaderService(ApiConfig apiConfig)
        {
            this.apiConfig = apiConfig;
        }

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
            var parentObject = new GameObject();
            string url = $"{GetBaseUrl().TrimEnd('/')}/{worldId}/{modelId}";
            await gltfHandler.Load(parentObject, url);
            return parentObject;
        }
    }
}
