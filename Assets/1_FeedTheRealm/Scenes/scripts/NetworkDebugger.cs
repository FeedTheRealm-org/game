using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralized debug logger for network and scene information.
/// Consolidates debugging info without multiple components.
/// </summary>
public class NetworkDebugger : MonoBehaviour
{
    public static NetworkDebugger Instance { get; private set; }

    [Header("Debug Settings")]
    [SerializeField] private bool enablePeriodicLogs = true;
    [SerializeField] private float logIntervalSeconds = 5f;
    [SerializeField] private bool logSceneChanges = true;
    [SerializeField] private bool logNetworkEvents = true;
    [SerializeField] private bool logBuildScenes = true;
    [SerializeField] private Logging.Logger logger;

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

        if (logNetworkEvents && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (logBuildScenes)
        {
            LogBuildScenes();
        }
    }

    private void Update()
    {
        if (enablePeriodicLogs && Time.time >= nextLogTime)
        {
            nextLogTime = Time.time + logIntervalSeconds;
            LogPeriodicStatus();
        }
    }

    private void LogStartupInfo()
    {
        logger.Log("=== NETWORK DEBUGGER INITIALIZED ===", this, Logging.LogType.Info);
        logger.Log($"Scene: {SceneManager.GetActiveScene().name}", this, Logging.LogType.Info);
        logger.Log($"NetworkManager exists: {NetworkManager.Singleton != null}", this, Logging.LogType.Info);

        if (NetworkManager.Singleton != null)
        {
            logger.Log($"NetworkManager scene: {NetworkManager.Singleton.gameObject.scene.name}", this, Logging.LogType.Info);
            logger.Log($"IsServer: {NetworkManager.Singleton.IsServer}", this, Logging.LogType.Info);
            logger.Log($"IsClient: {NetworkManager.Singleton.IsClient}", this, Logging.LogType.Info);
            logger.Log($"IsHost: {NetworkManager.Singleton.IsHost}", this, Logging.LogType.Info);
        }
    }

    private void LogBuildScenes()
    {
        logger.Log("=== BUILD SCENES ===", this, Logging.LogType.Info);
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        logger.Log($"Total scenes in build: {sceneCount}", this, Logging.LogType.Info);

        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            logger.Log($"  [{i}] {sceneName} ({scenePath})", this, Logging.LogType.Info);
        }
    }

    private void LogPeriodicStatus()
    {
        string scene = SceneManager.GetActiveScene().name;
        bool hasNetworkManager = NetworkManager.Singleton != null;
        
        string status = $"[{Time.time:F1}s] Scene: {scene} | NetworkManager: {hasNetworkManager}";
        
        if (hasNetworkManager)
        {
            status += $" | Server: {NetworkManager.Singleton.IsServer}";
            status += $" | Client: {NetworkManager.Singleton.IsClient}";
            status += $" | Players: {NetworkManager.Singleton.ConnectedClients.Count}";
        }

        logger.Log(status, this, Logging.LogType.Info);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        logger.Log($"🔁 Scene loaded: {scene.name} (mode: {mode})", this, Logging.LogType.Info);
        
        if (NetworkManager.Singleton != null)
        {
            logger.Log($"   Network status: Server={NetworkManager.Singleton.IsServer}, Client={NetworkManager.Singleton.IsClient}", this, Logging.LogType.Info);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        bool isLocal = NetworkManager.Singleton.LocalClientId == clientId;
        logger.Log($"👤 Client connected: {clientId} {(isLocal ? "(LOCAL)" : "(REMOTE)")}", this, Logging.LogType.Info);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        logger.Log($"👋 Client disconnected: {clientId}", this, Logging.LogType.Info);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }
    }

    // Public utility methods for manual logging
    public static void LogPlayerInfo(GameObject player)
    {
        if (Instance == null) return;

        var netObj = player.GetComponent<NetworkObject>();
        Instance.logger.Log($"📍 Player Info: {player.name}", Instance, Logging.LogType.Info);
        
        if (netObj != null)
        {
            Instance.logger.Log($"   NetworkId: {netObj.NetworkObjectId}", Instance, Logging.LogType.Info);
            Instance.logger.Log($"   Owner: {netObj.OwnerClientId}", Instance, Logging.LogType.Info);
            Instance.logger.Log($"   IsOwner: {netObj.IsOwner}", Instance, Logging.LogType.Info);
            Instance.logger.Log($"   IsSpawned: {netObj.IsSpawned}", Instance, Logging.LogType.Info);
        }
    }

    public static void LogCameraInfo(Transform target)
    {
        if (Instance == null) return;

        Instance.logger.Log($"🎥 Camera target set: {(target != null ? target.name : "NULL")}", Instance, Logging.LogType.Info);
        Instance.logger.Log($"   Main Camera: {(Camera.main != null ? Camera.main.name : "NULL")}", Instance, Logging.LogType.Info);
    }
}
