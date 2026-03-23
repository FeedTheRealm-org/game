using System;
using Cysharp.Threading.Tasks;
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
    [CreateAssetMenu(
        fileName = "GltLoaderService",
        menuName = "Scriptable Objects/API/GltLoaderService"
    )]
    public class GltLoaderService : ScriptableObject
    {
        [SerializeField]
        private ApiConfig apiConfig;

        /// <summary>
        /// Downloads and loads a GLB model from the GLTF API, instantiates it, and returns the GameObject.
        /// </summary>
        public async UniTask<GameObject> DownloadModel(string url)
        {
            string fullUrl = $"https://{apiConfig.ModelsCDN.TrimEnd('/')}/{url.TrimStart('/')}";
            var parentObject = new GameObject("Model");
            Debug.Log($"Downloading model from URL: {fullUrl}");
            await LoadModel(parentObject, fullUrl);
            return parentObject.transform.childCount > 0
                ? parentObject.transform.GetChild(0).gameObject
                : parentObject;
        }

        private async UniTask LoadModel(GameObject parent, string modelUrl)
        {
            if (string.IsNullOrEmpty(modelUrl))
            {
                CreateFallback(parent);
                return;
            }

            try
            {
                var gltfImport = new GltfImport();
                bool success = await gltfImport.Load(modelUrl);

                if (!success)
                {
                    Debug.LogWarning($"Failed to load: {modelUrl}");
                    CreateFallback(parent);
                    return;
                }

                await gltfImport.InstantiateMainSceneAsync(parent.transform);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GLTF load exception for '{modelUrl}': {exception.Message}");
                CreateFallback(parent);
            }
        }

        private void CreateFallback(GameObject parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent.transform);
        }
    }
}
