using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Script que inicia automáticamente un servidor dedicado en builds de servidor.
/// Se debe colocar en la escena inicial del servidor (ej: MultiplayerScene).
/// </summary>
public class ServerBootstrap : MonoBehaviour
{
    [Header("Server Configuration")]
    [SerializeField] private ushort defaultPort = 7777;
    [SerializeField] private string defaultAddress = "0.0.0.0"; // Escucha en todas las interfaces
    [SerializeField] private int maxPlayers = 10;
    
    [Header("Scene Configuration")]
    [SerializeField] private string gameSceneName = "MultiplayerScene";
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
        
        // Iniciar el servidor
        bool success = NetworkManager.Singleton.StartServer();
        
        if (success)
        {
            LogServerInfo($"✅ Dedicated Server started successfully!");
            LogServerInfo($"   Address: {address}");
            LogServerInfo($"   Port: {port}");
            LogServerInfo($"   Max Players: {maxConnections}");
            LogServerInfo($"   Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            
            // Cargar escena del juego si está configurado y no estamos ya en ella
            if (autoLoadGameScene && !string.IsNullOrEmpty(gameSceneName))
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (currentScene != gameSceneName)
                {
                    LogServerInfo($"Loading game scene: {gameSceneName}");
                    NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
                }
            }
        }
        else
        {
            Debug.LogError("[ServerBootstrap] ❌ Failed to start dedicated server!");
        }
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

        var connectionApproval = NetworkManager.Singleton.NetworkConfig.ConnectionApproval;
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = connectionApproval;
        
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

        // Configurar el transporte para escuchar en la dirección y puerto especificados
        transport.ConnectionData.Address = address;
        transport.ConnectionData.Port = port;
        transport.ConnectionData.ServerListenAddress = address;
        
        LogServerInfo($"UnityTransport configured: {address}:{port}");
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
            LogServerInfo("Shutting down server...");
        }
    }

    private void OnApplicationQuit()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            LogServerInfo("Application quitting - shutting down server...");
            NetworkManager.Singleton.Shutdown();
        }
    }

    #endregion
}
