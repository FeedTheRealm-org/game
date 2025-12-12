using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GLTFast;
using Models;
using API;
using World;
using Worlds;
using System.Threading.Tasks;
using System.IO;
using System;

public class WorldLoaderController : MonoBehaviour {

    [SerializeField] private WorldHandler worldHandler;
    [SerializeField] private ModelService modelService;
    [SerializeField] private Session.Session session;
    [SerializeField] private WorldController worldController;
    [SerializeField] private Logging.Logger logger;
    [SerializeField] private UIDocument loadingScreenUI;
    [SerializeField] private GameObject world;


    private async void Start() {

        logger.Log("Starting WorldLoaderController test...", this);
        await TestLoadFirstModel();
    }

    private async Task TestLoadFirstModel() {

        string testWorldId = worldHandler.selectedWorld.id;

        logger.Log("Fetching model IDs from world...", this);

        // 1. GET MODEL ID LIST
        List<string> modelIds = await modelService.ListWorldAssets(
            testWorldId,
            session.APIToken
        );

        if (modelIds.Count == 0) {
            logger.Log("No models found in world", this, Logging.LogType.Warning);
            return;
        }

        string modelId = modelIds[0]; // pick first one
        logger.Log("Model ID: " + modelId, this);

        // 2. BUILD API URL
        string url = $"http://localhost:8000/assets/models/{testWorldId}/{modelId}";
        logger.Log("Downloading model from: " + url, this);
        // 3. LOAD GLB USING GLTFast
        var gltf = new GltfImport();

        bool loaded = await gltf.Load(url);

        if (!loaded) {
            logger.Log("Failed to download or parse GLB from API", this, Logging.LogType.Error);
            return;
        }

        // 4. INSTANTIATE MODEL AT (0,0,0)
        bool instantiated = await gltf.InstantiateMainSceneAsync(world.transform);

        if (!instantiated) {
            logger.Log("GLTFast failed to instantiate model.", this, Logging.LogType.Error);
            return;
        }

        // Get the instantiated model (first child of this transform)
        GameObject instance = world.transform.GetChild(0).gameObject;
        instance.transform.position = Vector3.zero;
        instance.transform.localScale = Vector3.one;
        instance.name = "Downloaded_Model_Test";

        logger.Log("Model loaded and instantiated successfully!", this);
        loadingScreenUI.gameObject.SetActive(false);
    }


}
