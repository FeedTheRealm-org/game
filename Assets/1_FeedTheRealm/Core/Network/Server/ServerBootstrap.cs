using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Items;

/// <summary>
/// Script that automatically starts a dedicated server on server builds.
/// Should be placed in the server's initial scene (e.g.: MultiplayerScene).
/// </summary>
public class ServerBootstrap : MonoBehaviour {
    [Header("Server Configuration")]
    [SerializeField] private ushort defaultPort = 7777;
    [SerializeField] private string defaultAddress = "0.0.0.0";
    [SerializeField] private int maxPlayers = 10;

    [Header("Scene Configuration")]
    [SerializeField] private SceneReference gameScene;
    [SerializeField] private bool autoLoadGameScene = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    private void Start() {
        // Only execute on server builds (not on client or editor by default)
#if UNITY_SERVER && !UNITY_EDITOR
        StartDedicatedServer();
#else
        if (verboseLogging) {
            Debug.Log("[ServerBootstrap] Not running as dedicated server. Use UNITY_SERVER define for server builds.");
        }
#endif
    }

    /// <summary>
    /// Starts the dedicated server with configuration from command line arguments or default values
    /// </summary>
    private void StartDedicatedServer() {
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
        NetworkManager.Singleton.OnClientConnectedCallback += OnServerClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnServerClientDisconnected;

        LogServerInfo("📡 Subscribed to connection events");

        // Start the server
        bool success = NetworkManager.Singleton.StartServer();

        if (success) {
            LogServerInfo($"✅ Dedicated Server started successfully!");
            LogServerInfo($"   Address: {address}");
            LogServerInfo($"   Port: {port}");
            LogServerInfo($"   Max Players: {maxConnections}");
            LogServerInfo($"   Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            LogServerInfo($"   🔊 Server is now LISTENING for connections...");

            // Wait for ItemsManager to initialize before loading game scene
            StartCoroutine(WaitForItemsManagerThenLoadScene());
        } else {
            Debug.LogError("[ServerBootstrap] ❌ Failed to start dedicated server!");
        }
    }

    /// <summary>
    /// Callback when a client connects to the server
    /// </summary>
    private void OnServerClientConnected(ulong clientId) {
        LogServerInfo($"🎉 CLIENT CONNECTED! ClientId: {clientId}");
        LogServerInfo($"   Total clients connected: {NetworkManager.Singleton.ConnectedClientsList.Count}");
    }

    /// <summary>
    /// Callback when a client disconnects from the server
    /// </summary>
    private void OnServerClientDisconnected(ulong clientId) {
        LogServerInfo($"👋 CLIENT DISCONNECTED! ClientId: {clientId}");
        LogServerInfo($"   Remaining clients: {NetworkManager.Singleton.ConnectedClientsList.Count}");
    }

    /// <summary>
    /// Configures NetworkManager with server parameters
    /// </summary>
    private void ConfigureNetworkManager(int maxConnections) {
        if (NetworkManager.Singleton == null) {
            Debug.LogError("[ServerBootstrap] NetworkManager.Singleton is null! Make sure NetworkManager exists in the scene.");
            return;
        }

        LogServerInfo($"NetworkManager configured with max {maxConnections} connections");
    }

    /// <summary>
    /// Configures Unity Transport with the specified address and port
    /// </summary>
    private void ConfigureTransport(string address, ushort port) {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null) {
            Debug.LogError("[ServerBootstrap] UnityTransport component not found on NetworkManager!");
            return;
        }

        // - ServerListenAddress: The interface where the server LISTENS (0.0.0.0 = all)
        // - Port: The port to listen on
        // - Address: NOT used on server (only for clients)

        transport.ConnectionData.ServerListenAddress = address;  // 0.0.0.0 to listen on all interfaces
        transport.ConnectionData.Port = port;                     // Port 7777
        transport.ConnectionData.Address = string.Empty;         // Empty on server (not used)

        LogServerInfo($"🔧 UnityTransport configured for SERVER:");
        LogServerInfo($"   → ServerListenAddress: {transport.ConnectionData.ServerListenAddress} (listening on all interfaces)");
        LogServerInfo($"   → Port: {transport.ConnectionData.Port}");
        LogServerInfo($"   → Address: '{transport.ConnectionData.Address}' (not used by server)");

        // Configuration with ServerListenAddress="0.0.0.0" allows external connections
        // Make sure "Allow Remote Connections" is enabled in Unity Transport Inspector
        LogServerInfo($"   → 🌐 ATTENTION: Verify that 'Allow Remote Connections' is enabled in Unity Transport!");
    }

    #region Command Line Arguments Parsing

    /// <summary>
    /// Reads the port from command line arguments: -port 7777
    /// </summary>
    private ushort GetPortFromArgs() {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) {
            if (args[i] == "-port" || args[i] == "--port") {
                if (ushort.TryParse(args[i + 1], out ushort port)) {
                    LogServerInfo($"Port from command line: {port}");
                    return port;
                } else {
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
    private string GetAddressFromArgs() {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) {
            if (args[i] == "-address" || args[i] == "--address" || args[i] == "-ip") {
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
    private int GetMaxPlayersFromArgs() {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) {
            if (args[i] == "-maxplayers" || args[i] == "--maxplayers" || args[i] == "-max") {
                if (int.TryParse(args[i + 1], out int max)) {
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
    private System.Collections.IEnumerator WaitForItemsManagerThenLoadScene() {
        // Check if DedicatedServerItemsManager is already initialized (should be from MPMenuScene)
        if (Items.DedicatedServerItemsManager.Instance != null &&
            Items.DedicatedServerItemsManager.Instance.IsInitialized) {
            LogServerInfo($"✅ ItemsManager already initialized with {Items.DedicatedServerItemsManager.Instance.TotalItemsLoaded} items (from persistent scene)");
        } else {
            // ItemsManager not ready yet - wait a bit (should initialize from ItemsManagerBootstrap in MPMenuScene)
            LogServerInfo($"⏳ Waiting for ItemsManager initialization from MPMenuScene...");

            float timeout = 10f;
            float elapsed = 0f;

            while (elapsed < timeout) {
                if (Items.DedicatedServerItemsManager.Instance != null &&
                    Items.DedicatedServerItemsManager.Instance.IsInitialized) {
                    LogServerInfo($"✅ ItemsManager initialized with {Items.DedicatedServerItemsManager.Instance.TotalItemsLoaded} items!");
                    break;
                }

                yield return new UnityEngine.WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (elapsed >= timeout) {
                Debug.LogWarning("[ServerBootstrap] ⚠️ ItemsManager not found! Make sure ItemsManagerBootstrap exists in MPMenuScene. Loading scene anyway...");
            }
        }

        // Load game scene if configured and we're not already in it
        if (autoLoadGameScene && gameScene != null && !string.IsNullOrEmpty(gameScene.SceneName)) {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != gameScene.SceneName) {
                LogServerInfo($"Loading game scene: {gameScene.SceneName}");
                NetworkManager.Singleton.SceneManager.LoadScene(gameScene.SceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
    }

    #region Logging

    private void LogServerInfo(string message) {
        if (verboseLogging) {
            Debug.Log($"[ServerBootstrap] {message}");
        }
    }

    #endregion

    #region Server Lifecycle

    private void OnDestroy() {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) {
            // Unsubscribe from events
            NetworkManager.Singleton.OnClientConnectedCallback -= OnServerClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnServerClientDisconnected;

            LogServerInfo("Shutting down server...");
        }
    }

    private void OnApplicationQuit() {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) {
            // Unsubscribe from events
            NetworkManager.Singleton.OnClientConnectedCallback -= OnServerClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnServerClientDisconnected;

            LogServerInfo("Application quitting - shutting down server...");
            NetworkManager.Singleton.Shutdown();
        }
    }

    #endregion
}
