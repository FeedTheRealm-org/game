using System;
using Cysharp.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace API
{
    /// <summary>
    ///  This is a more streamlined handler for loading GLTF FTRShared.Runtime.Models. This come directly from the world editor client code.
    ///  This could not be placed in the shared packaged due to dependency issues with GLTFast in the package context.
    /// </summary>
    public sealed class GltfHandler
    {
        /// <summary>
        /// Loads a GLTF model into the provided parent GameObject.
        /// </summary>
        public async UniTask Load(GameObject parent, string modelUrl)
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
