using System.Threading.Tasks;
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
        private GltfImport gltf = new();

        /// <summary>
        /// Loads a GLTF model into the provided parent GameObject.
        /// </summary>
        public async Task Load(GameObject parent, string modelUrl)
        {
            if (string.IsNullOrEmpty(modelUrl))
            {
                CreateFallback(parent);
                return;
            }

            bool success = await gltf.Load(modelUrl);

            if (!success)
            {
                Debug.LogWarning($"Failed to load: {modelUrl}");
                CreateFallback(parent);
                return;
            }
            await gltf.InstantiateMainSceneAsync(parent.transform);
        }

        private void CreateFallback(GameObject parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent.transform);
        }
    }
}
