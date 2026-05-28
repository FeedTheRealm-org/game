using System;
using Cysharp.Threading.Tasks;
using FTRShared.Runtime.Core.Interfaces;
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
    public class GltLoaderService : ScriptableObject, IGltfLoader
    {
        [SerializeField]
        private ApiConfig apiConfig;

        /// <summary>
        /// Downloads and loads a GLB model from the GLTF API, instantiates it, and returns the GameObject.
        /// </summary>
        public async UniTask<GameObject> LoadModel(byte[] data)
        {
            var parent = new GameObject("ModelContainer");

            if (data == null || data.Length == 0)
            {
                CreateFallback(parent);
                return parent;
            }

            try
            {
                var gltfImport = new GltfImport();
                bool success = await gltfImport.Load(data);

                if (!success)
                {
                    Debug.LogWarning($"Failed to load model");
                    CreateFallback(parent);
                    return parent;
                }

                await gltfImport.InstantiateMainSceneAsync(parent.transform);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GLTF load exception: {exception.Message}");
                CreateFallback(parent);
            }

            return parent.transform.childCount > 0
                ? parent.transform.GetChild(0).gameObject
                : parent;
        }

        private void CreateFallback(GameObject parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent.transform);
        }
    }
}
