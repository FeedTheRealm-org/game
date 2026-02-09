using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace API
{
    /// <summary>
    ///  This is a more streamlined handler for loading GLTF models. This come directly from the world editor client code.
    ///  This could not be placed in the shared packaged due to dependency issues with GLTFast in the package context.
    /// </summary>
    public static class GltfHandler
    {
        private const string MODELS_FOLDER = "Models";
        private const string GLTF_EXTENSION = ".glb";
        private const string FILE_PROTOCOL = "file://";

        /// <summary>
        /// Loads a GLTF model into the provided parent GameObject.
        /// </summary>
        public static async Task Load(
            GameObject parent,
            string modelName,
            bool useLocalAsset = true,
            bool addColliders = true
        )
        {
            if (string.IsNullOrEmpty(modelName))
            {
                CreateFallback(parent);
                return;
            }
            string modelPath = useLocalAsset
                ? FILE_PROTOCOL
                    + Path.Combine(
                        Application.streamingAssetsPath,
                        MODELS_FOLDER,
                        modelName + GLTF_EXTENSION
                    )
                : modelName;

            var gltf = new GltfImport();
            bool success = await gltf.Load(modelPath);

            if (!success)
            {
                Debug.LogWarning($"Failed to load: {modelPath}");
                CreateFallback(parent);
                return;
            }
            await gltf.InstantiateMainSceneAsync(parent.transform);
            NormalizeChildren(parent);
            FlattenHierarchy(parent);
            if (addColliders)
                AddColliders(parent);
        }

        private static void CreateFallback(GameObject parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent.transform);
            cube.name = "Fallback";
        }

        // TODO: review this method to improve performance, this seems more like an unecesary edge case solution
        private static void FlattenHierarchy(GameObject parent)
        {
            Transform[] children = parent.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child != parent.transform && child.childCount == 0)
                {
                    // Move renderers to parent if they have no children
                    if (child.GetComponent<Renderer>() != null)
                    {
                        child.SetParent(parent.transform);
                    }
                }
            }
        }

        private static void NormalizeChildren(GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;
            }
        }

        private static void AddColliders(GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                if (
                    child.GetComponent<Renderer>() != null
                    && child.GetComponent<Collider>() == null
                )
                {
                    child.gameObject.AddComponent<BoxCollider>();
                }
            }
        }
    }
}
