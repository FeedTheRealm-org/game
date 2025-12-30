using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralized debug logger for network and scene information.
/// Consolidates debugging info without multiple components.
/// </summary>
public class NetworkDebugger : MonoBehaviour
{
    public static NetworkDebugger Instance { get; private set; }

    [Header("Debug Settings")]
    [SerializeField]
    private bool enablePeriodicLogs = true;

    [SerializeField]
    private float logIntervalSeconds = 10f;

    [SerializeField]
    private bool logSceneChanges = true;

    [SerializeField]
    private bool logNetworkEvents = true;

    [SerializeField]
    private bool logBuildScenes = true;

    [SerializeField]
    private Logging.Logger logger;

    private float nextLogTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LogStartupInfo();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (logSceneChanges)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        if (logNetworkEvents)
        {
            NetworkServer.OnConnectedEvent += OnClientConnected;
            NetworkServer.OnDisconnectedEvent += OnClientDisconnected;
        }

        if (logBuildScenes)
        {
            LogBuildScenes();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Unsubscribe from Mirror network events
            if (NetworkServer.active)
            {
                NetworkServer.OnConnectedEvent -= OnClientConnected;
                NetworkServer.OnDisconnectedEvent -= OnClientDisconnected;
            }

            Instance = null;
        }
    }

    private void LogStartupInfo()
    {
        logger.Log("=== NETWORK DEBUGGER INITIALIZED ===", this);
        logger.Log($"Scene: {SceneManager.GetActiveScene().name}", this);
        logger.Log($"NetworkManager exists: {NetworkManager.singleton != null}", this);

        if (NetworkManager.singleton != null)
        {
            logger.Log(
                $"NetworkManager scene: {NetworkManager.singleton.gameObject.scene.name}",
                this
            );
            logger.Log($"IsServer: {NetworkServer.active}", this);
            logger.Log($"IsClient: {NetworkClient.isConnected}", this);
            logger.Log($"IsHost: {NetworkServer.active && NetworkClient.isConnected}", this);
        }
    }

    private void LogBuildScenes()
    {
        logger.Log("=== BUILD SCENES ===", this);
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        logger.Log($"Total scenes in build: {sceneCount}", this);

        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            logger.Log($"  [{i}] {sceneName} ({scenePath})", this);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        logger.Log($"🔁 Scene loaded: {scene.name} (mode: {mode})", this);

        if (NetworkManager.singleton != null)
        {
            logger.Log(
                $"   Network status: Server={NetworkServer.active}, Client={NetworkClient.isConnected}",
                this
            );
        }
    }

    private void OnClientConnected(NetworkConnectionToClient conn)
    {
        logger.Log(
            $"👤 Client connected: {conn.connectionId}, Total conns: {NetworkServer.connections.Count}",
            this
        );
    }

    private void OnClientDisconnected(NetworkConnectionToClient conn)
    {
        logger.Log(
            $"👤 Client disconnected: {conn.connectionId}, Total conns: {NetworkServer.connections.Count}",
            this
        );
    }
}
