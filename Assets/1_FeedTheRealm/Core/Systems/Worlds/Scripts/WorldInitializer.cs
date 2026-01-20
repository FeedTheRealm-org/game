using System.ComponentModel;
using System.Linq;
using Models;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// This is a helpful tool for locally instantiating and testing worlds during development.
/// It uses the WorldLoaderControllerV2 to load the world data and assets,
/// I reccomend using this as a baseline when integrating world loading into other parts of the game
/// </summary>
public class WorldInitializer : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField]
    private UIDocument loadingScreenUI;

    [Header("World Loader Controller")]
    [SerializeField]
    private WorldLoaderControllerV2 worldLoaderController;

    [Header("Debug Settings")]
    [Description(
        "Here you can set the world ID and access token for debugging purposes. Also add the player GameObject to be spawned in the world."
    )]
    [SerializeField]
    private string worldId;

    [SerializeField]
    private string accessToken;

    [SerializeField]
    private GameObject player;

    async void Start()
    {
        loadingScreenUI.gameObject.SetActive(true);
        WorldData worldData = await worldLoaderController.LoadWorld(worldId, accessToken);

        if (player != null && worldData != null && worldData.playerSpawnAreas.Count > 0)
        {
            int randomIndex = Random.Range(0, worldData.playerSpawnAreas.Count);
            Debug.Log(
                $"Available spawnpoints: {string.Join(", ", worldData.playerSpawnAreas.Select(s => s.Position))}"
            );
            Vector3 spawnPosition = worldData.playerSpawnAreas[randomIndex].Position;
            spawnPosition.y = 0f;
            player.transform.position = spawnPosition;
            Debug.Log($"Player spawned at: {spawnPosition}");
        }
        loadingScreenUI.gameObject.SetActive(false);
    }
}
