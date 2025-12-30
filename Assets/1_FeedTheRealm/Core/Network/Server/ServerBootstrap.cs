using Items;
using kcp2k;
using Mirror;
using UnityEngine;

/// <summary>
/// Script that automatically starts a dedicated server on server builds.
/// Should be placed in the server's initial scene (e.g.: MultiplayerScene).
/// </summary>
public class ServerBootstrap : MonoBehaviour
{
    [Header("Server Configuration")]
    [SerializeField]
    private ushort defaultPort = 7777;

    [SerializeField]
    private string defaultAddress = "0.0.0.0";

    [SerializeField]
    private int maxPlayers = 10;

    [Header("Scene Configuration")]
    [SerializeField]
    private SceneReference gameScene;

    [SerializeField]
    private bool autoLoadGameScene = true;

    [Header("Debug")]
    [SerializeField]
    private bool verboseLogging = true;

    private void Start()
    {
        // Only execute on server builds (not on client or editor by default)
#if UNITY_SERVER && !UNITY_EDITOR
        StartDedicatedServer();
#else
        if (verboseLogging)
        {
            Debug.Log(
                "[ServerBootstrap] Not running as dedicated server. Use UNITY_SERVER define for server builds."
            );
        }
#endif
    }

    /// <summary>
    /// Starts the dedicated server with configuration from command line arguments or default values
    /// </summary>
    private void StartDedicatedServer()
    {
        LogServerInfo("Initializing Dedicated Server...");

        // Get configuration from command line arguments
        ushort port = GetPortFromArgs();
        string address = GetAddressFromArgs();
        int maxConnections = GetMaxPlayersFromArgs();

        // Configure NetworkManager
        ConfigureNetworkManager(maxConnections);

        // Configure transport (Unity Transport)
        ConfigureTransport(address, port);

        // Subscribe to connection events BEFORE starting the server
        NetworkServer.OnConnectedEvent += OnServerClientConnected;
        NetworkServer.OnDisconnectedEvent += OnServerClientDisconnected;

        LogServerInfo("📡 Subscribed to connection events");

        // Start the server
        NetworkManager.singleton.StartServer();
        bool success = NetworkServer.active;

        if (success)
        {
            LogServerInfo($"✅ Dedicated Server started successfully!");
            LogServerInfo($"   Address: {address}");
            LogServerInfo($"   Port: {port}");
            LogServerInfo($"   Max Players: {maxConnections}");
            LogServerInfo(
                $"   Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}"
            );
            LogServerInfo($"   🔊 Server is now LISTENING for connections...");

            // Wait for ItemsManager to initialize before loading game scene
            StartCoroutine(WaitForItemsManagerThenLoadScene());
        }
        else
        {
            Debug.LogError("[ServerBootstrap] ❌ Failed to start dedicated server!");
        }
    }

    /// <summary>
    /// Callback when a client connects to the server
    /// </summary>
    private void OnServerClientConnected(NetworkConnectionToClient conn)
    {
        LogServerInfo($"🎉 CLIENT CONNECTED! ConnectionId: {conn.connectionId}");
        LogServerInfo($"   Total clients connected: {NetworkServer.connections.Count}");
    }

    /// <summary>
    /// Callback when a client disconnects from the server
    /// </summary>
    private void OnServerClientDisconnected(NetworkConnectionToClient conn)
    {
        LogServerInfo($"👋 CLIENT DISCONNECTED! ConnectionId: {conn.connectionId}");
        LogServerInfo($"   Remaining clients: {NetworkServer.connections.Count}");
    }

    /// <summary>
    /// Configures NetworkManager with server parameters
    /// </summary>
    private void ConfigureNetworkManager(int maxConnections)
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError(
                "[ServerBootstrap] NetworkManager.singleton is null! Make sure NetworkManager exists in the scene."
            );
            return;
        }

        NetworkManager.singleton.maxConnections = maxConnections;
        LogServerInfo($"NetworkManager configured with max {maxConnections} connections");
    }

    /// <summary>
    /// Configures KCP Transport with the specified port
    /// </summary>
    private void ConfigureTransport(string address, ushort port)
    {
        var transport = NetworkManager.singleton.GetComponent<KcpTransport>();

        if (transport == null)
        {
            Debug.LogError("[ServerBootstrap] KcpTransport component not found on NetworkManager!");
            return;
        }

        // KCP Transport configuration for server
        // - Port: The port to listen on
        // - KCP always listens on 0.0.0.0 (all interfaces) - no need to configure address

        transport.Port = port;

        LogServerInfo($"🔧 KcpTransport configured for SERVER:");
        LogServerInfo($"   → Port: {transport.Port}");
        LogServerInfo($"   → Listening on all interfaces (0.0.0.0)");
        LogServerInfo(
            $"   → Address parameter '{address}' not used (KCP listens on 0.0.0.0 by default)"
        );
    }

    #region Command Line Arguments Parsing

    /// <summary>
    /// Reads the port from command line arguments: -port 7777
    /// </summary>
    private ushort GetPortFromArgs()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-port" || args[i] == "--port")
            {
                if (ushort.TryParse(args[i + 1], out ushort port))
                {
                    LogServerInfo($"Port from command line: {port}");
                    return port;
                }
                else
                {
                    Debug.LogWarning($"[ServerBootstrap] Invalid port argument: {args[i + 1]}");
                }
            }
        }

        LogServerInfo($"Using default port: {defaultPort}");
        return defaultPort;
    }

    /// <summary>
    /// Reads the address from command line arguments: -address 0.0.0.0
    /// </summary>
    private string GetAddressFromArgs()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-address" || args[i] == "--address" || args[i] == "-ip")
            {
                LogServerInfo($"Address from command line: {args[i + 1]}");
                return args[i + 1];
            }
        }

        LogServerInfo($"Using default address: {defaultAddress}");
        return defaultAddress;
    }

    /// <summary>
    /// Reads the maximum players from command line arguments: -maxplayers 10
    /// </summary>
    private int GetMaxPlayersFromArgs()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-maxplayers" || args[i] == "--maxplayers" || args[i] == "-max")
            {
                if (int.TryParse(args[i + 1], out int max))
                {
                    LogServerInfo($"Max players from command line: {max}");
                    return max;
                }
            }
        }

        return maxPlayers;
    }

    #endregion

    /// <summary>
    /// Wait for ItemsManager to be initialized before loading the game scene.
    /// ItemsManager should be initialized in MPMenuScene and persist via DontDestroyOnLoad.
    /// </summary>
    private System.Collections.IEnumerator WaitForItemsManagerThenLoadScene()
    {
        // Check if DedicatedServerItemsManager is already initialized (should be from MPMenuScene)
        if (
            Items.DedicatedServerItemsManager.Instance != null
            && Items.DedicatedServerItemsManager.Instance.IsInitialized
        )
        {
            LogServerInfo(
                $"✅ ItemsManager already initialized with {Items.DedicatedServerItemsManager.Instance.TotalItemsLoaded} items (from persistent scene)"
            );
        }
        else
        {
            // ItemsManager not ready yet - wait a bit (should initialize from ItemsManagerBootstrap in MPMenuScene)
            LogServerInfo($"⏳ Waiting for ItemsManager initialization from MPMenuScene...");

            float timeout = 10f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (
                    Items.DedicatedServerItemsManager.Instance != null
                    && Items.DedicatedServerItemsManager.Instance.IsInitialized
                )
                {
                    LogServerInfo(
                        $"✅ ItemsManager initialized with {Items.DedicatedServerItemsManager.Instance.TotalItemsLoaded} items!"
                    );
                    break;
                }

                yield return new UnityEngine.WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (elapsed >= timeout)
            {
                Debug.LogWarning(
                    "[ServerBootstrap] ⚠️ ItemsManager not found! Make sure ItemsManagerBootstrap exists in MPMenuScene. Loading scene anyway..."
                );
            }
        }

        // Load game scene if configured and we're not already in it
        if (autoLoadGameScene && gameScene != null && !string.IsNullOrEmpty(gameScene.SceneName))
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != gameScene.SceneName)
            {
                LogServerInfo($"Loading game scene: {gameScene.SceneName}");
                NetworkManager.singleton.ServerChangeScene(gameScene.SceneName);
            }
        }
    }

    #region Logging

    private void LogServerInfo(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[ServerBootstrap] {message}");
        }
    }

    #endregion

    #region Server Lifecycle

    private void OnDestroy()
    {
        if (NetworkServer.active)
        {
            // Unsubscribe from events
            NetworkServer.OnConnectedEvent -= OnServerClientConnected;
            NetworkServer.OnDisconnectedEvent -= OnServerClientDisconnected;

            LogServerInfo("Shutting down server...");
        }
    }

    private void OnApplicationQuit()
    {
        if (NetworkServer.active)
        {
            // Unsubscribe from events
            NetworkServer.OnConnectedEvent -= OnServerClientConnected;
            NetworkServer.OnDisconnectedEvent -= OnServerClientDisconnected;

            LogServerInfo("Application quitting - shutting down server...");
            NetworkManager.singleton.StopServer();
        }
    }

    #endregion
}
