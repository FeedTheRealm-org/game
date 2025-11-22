using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Items;

/// <summary>
/// Script que inicia automáticamente un servidor dedicado en builds de servidor.
/// Se debe colocar en la escena inicial del servidor (ej: MultiplayerScene).
/// </summary>
public class ServerBootstrap : MonoBehaviour
{
    [Header("Server Configuration")]
    [SerializeField] private ushort defaultPort = 7777;
    [SerializeField] private string defaultAddress = "0.0.0.0";
    [SerializeField] private int maxPlayers = 10;
    
    [Header("Scene Configuration")]
    [SerializeField] private SceneReference gameScene;
    [SerializeField] private bool autoLoadGameScene = true;
    
    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    private void Start()
    {
        // Solo ejecutar en builds de servidor (no en cliente ni editor por defecto)
        #if UNITY_SERVER && !UNITY_EDITOR
        StartDedicatedServer();
        #else
        if (verboseLogging)
        {
            Debug.Log("[ServerBootstrap] Not running as dedicated server. Use UNITY_SERVER define for server builds.");
        }
        #endif
    }

    /// <summary>
    /// Inicia el servidor dedicado con configuración desde argumentos de línea de comando o valores por defecto
    /// </summary>
    private void StartDedicatedServer()
    {
        LogServerInfo("Initializing Dedicated Server...");
        
        // Obtener configuración desde argumentos de línea de comando
        ushort port = GetPortFromArgs();
        string address = GetAddressFromArgs();
        int maxConnections = GetMaxPlayersFromArgs();
        
        // Configurar NetworkManager
        ConfigureNetworkManager(maxConnections);
        
        // Configurar el transporte (Unity Transport)
        ConfigureTransport(address, port);
        
        // Suscribirse a eventos de conexión ANTES de iniciar el servidor
        NetworkManager.Singleton.OnClientConnectedCallback += OnServerClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnServerClientDisconnected;
        
        LogServerInfo("📡 Subscribed to connection events");
        
        // Iniciar el servidor
        bool success = NetworkManager.Singleton.StartServer();
        
        if (success)
        {
            LogServerInfo($"✅ Dedicated Server started successfully!");
            LogServerInfo($"   Address: {address}");
            LogServerInfo($"   Port: {port}");
            LogServerInfo($"   Max Players: {maxConnections}");
            LogServerInfo($"   Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
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
    /// Callback cuando un cliente se conecta al servidor
    /// </summary>
    private void OnServerClientConnected(ulong clientId)
    {
        LogServerInfo($"🎉 CLIENT CONNECTED! ClientId: {clientId}");
        LogServerInfo($"   Total clients connected: {NetworkManager.Singleton.ConnectedClientsList.Count}");
    }
    
    /// <summary>
    /// Callback cuando un cliente se desconecta del servidor
    /// </summary>
    private void OnServerClientDisconnected(ulong clientId)
    {
        LogServerInfo($"👋 CLIENT DISCONNECTED! ClientId: {clientId}");
        LogServerInfo($"   Remaining clients: {NetworkManager.Singleton.ConnectedClientsList.Count}");
    }

    /// <summary>
    /// Configura el NetworkManager con los parámetros del servidor
    /// </summary>
    private void ConfigureNetworkManager(int maxConnections)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[ServerBootstrap] NetworkManager.Singleton is null! Make sure NetworkManager exists in the scene.");
            return;
        }

        LogServerInfo($"NetworkManager configured with max {maxConnections} connections");
    }

    /// <summary>
    /// Configura el Unity Transport con la dirección y puerto especificados
    /// </summary>
    private void ConfigureTransport(string address, ushort port)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        
        if (transport == null)
        {
            Debug.LogError("[ServerBootstrap] UnityTransport component not found on NetworkManager!");
            return;
        }

        // - ServerListenAddress: La interfaz donde el servidor ESCUCHA (0.0.0.0 = todas)
        // - Port: El puerto donde escuchar
        // - Address: NO se usa en el servidor (solo para clientes)
        
        transport.ConnectionData.ServerListenAddress = address;  // 0.0.0.0 para escuchar en todas las interfaces
        transport.ConnectionData.Port = port;                     // Puerto 7777
        transport.ConnectionData.Address = string.Empty;         // Vacío en el servidor (no se usa)
        
        LogServerInfo($"🔧 UnityTransport configured for SERVER:");
        LogServerInfo($"   → ServerListenAddress: {transport.ConnectionData.ServerListenAddress} (listening on all interfaces)");
        LogServerInfo($"   → Port: {transport.ConnectionData.Port}");
        LogServerInfo($"   → Address: '{transport.ConnectionData.Address}' (not used by server)");
        
        // La configuración con ServerListenAddress="0.0.0.0" permite conexiones externas
        // Asegúrate de que "Allow Remote Connections" esté habilitado en Unity Transport Inspector
        LogServerInfo($"   → 🌐 ATENTION: Verify that 'Allow Remote Connections' is enabled in Unity Transport!");
    }

    #region Command Line Arguments Parsing

    /// <summary>
    /// Lee el puerto desde los argumentos de línea de comando: -port 7777
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
    /// Lee la dirección desde los argumentos de línea de comando: -address 0.0.0.0
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
    /// Lee el máximo de jugadores desde los argumentos de línea de comando: -maxplayers 10
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
    /// This ensures loot drops work immediately when enemies die.
    /// </summary>
    private System.Collections.IEnumerator WaitForItemsManagerThenLoadScene()
    {
        LogServerInfo($"⏳ Waiting for ItemsManager initialization...");
        
        // Wait for DedicatedServerItemsManager to be initialized (max 10 seconds)
        float timeout = 10f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // Check if DedicatedServerItemsManager exists and is initialized
            if (Items.DedicatedServerItemsManager.Instance != null && 
                Items.DedicatedServerItemsManager.Instance.IsInitialized)
            {
                LogServerInfo($"✅ ItemsManager ready with {Items.DedicatedServerItemsManager.Instance.TotalItemsLoaded} items!");
                break;
            }
            
            yield return new UnityEngine.WaitForSeconds(0.1f); // Wait 100ms
            elapsed += 0.1f;
        }
        
        if (elapsed >= timeout)
        {
            Debug.LogWarning("[ServerBootstrap] ⚠️ ItemsManager initialization timeout! Loading scene anyway...");
        }
        
        // Cargar escena del juego si está configurado y no estamos ya en ella
        if (autoLoadGameScene && gameScene != null && !string.IsNullOrEmpty(gameScene.SceneName))
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != gameScene.SceneName)
            {
                LogServerInfo($"Loading game scene: {gameScene.SceneName}");
                NetworkManager.Singleton.SceneManager.LoadScene(gameScene.SceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            // Desuscribirse de eventos
            NetworkManager.Singleton.OnClientConnectedCallback -= OnServerClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnServerClientDisconnected;
            
            LogServerInfo("Shutting down server...");
        }
    }

    private void OnApplicationQuit()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            // Desuscribirse de eventos
            NetworkManager.Singleton.OnClientConnectedCallback -= OnServerClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnServerClientDisconnected;
            
            LogServerInfo("Application quitting - shutting down server...");
            NetworkManager.Singleton.Shutdown();
        }
    }

    #endregion
}
