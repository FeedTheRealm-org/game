using Mirror;
using kcp2k;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maneja el ciclo de vida de la conexión de red y coordina el loading screen.
/// Este componente persiste entre cambios de escena usando DontDestroyOnLoad.
/// Los valores de IP y puerto son pasados desde el UI (MultiplayerMenuController).
/// </summary>
public class NetworkConnectionHandler : MonoBehaviour
{
    private static NetworkConnectionHandler instance;
    public static NetworkConnectionHandler Instance => instance;
    
    [Header("Debug")]
    [SerializeField] private Logging.Logger logger;
    [SerializeField] private bool enableLogging = true;

    private bool isConnecting = false;
    
    private bool isSubscribedToEvents = false;
    private bool isSubscribedToSceneEvents = false;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            LogInfo($"⚠️ DUPLICATE INSTANCE DETECTED! Destroying this instance. Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        LogInfo($"NetworkConnectionHandler initialized with DontDestroyOnLoad in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            UnsubscribeFromNetworkEvents();
            instance = null;
        }
    }
    
    private void SubscribeToNetworkEvents()
    {
        if (NetworkManager.singleton == null)
        {
            LogError("Cannot subscribe to network events: NetworkManager.singleton is null!");
            return;
        }

        if (isSubscribedToEvents)
        {
            LogInfo("Already subscribed to network events, skipping...");
            return;
        }

        LogInfo("Subscribing to Network events...");

        NetworkClient.OnConnectedEvent += OnClientConnected;
        NetworkClient.OnDisconnectedEvent += OnClientDisconnected;

        // Mirror uses Unity's SceneManager.sceneLoaded instead of network-specific scene events
        SceneManager.sceneLoaded += OnSceneLoaded;

        isSubscribedToEvents = true;
    }
    
    // Mirror doesn't need separate scene event subscription - using Unity's SceneManager instead
    // This method is kept for compatibility but does nothing
    private void TrySubscribeToSceneEvents()
    {
        // Scene events are now handled via Unity's SceneManager.sceneLoaded
        // Subscribed in SubscribeToNetworkEvents()
    }
    

    private void UnsubscribeFromNetworkEvents()
    {
        if (!isSubscribedToEvents)
        {
            return;
        }

        NetworkClient.OnConnectedEvent -= OnClientConnected;
        NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        isSubscribedToEvents = false;
        LogInfo("Unsubscribed from network events");
    }
    
    #region Public Methods
    
    /// <summary>
    /// Conecta al servidor con IP y puerto específicos.
    /// Este método debe ser llamado desde el UI (ej: MultiplayerMenuController).
    /// </summary>
    public void ConnectToServer(string ipAddress, ushort port)
    {
        LogInfo("==============================================");
        LogInfo($"🔌 ConnectToServer() called with:");
        LogInfo($"   IP Address: {ipAddress}");
        LogInfo($"   Port: {port}");
        LogInfo("==============================================");

        if (NetworkManager.singleton == null)
        {
            LogError("NetworkManager.singleton is null!");
            return;
        }

        if (NetworkClient.isConnected)
        {
            LogWarning("Already connected or connecting!");
            return;
        }

        // Verificar que la IP no sea localhost si estamos intentando conectar a un servidor remoto
        if (ipAddress == "127.0.0.1" || ipAddress == "localhost")
        {
            LogWarning("⚠️ Connecting to localhost - this will only work if server is on the same machine!");
        }

        LogInfo($"Attempting to connect to server at {ipAddress}:{port}");

        // Suscribirse a eventos de red justo antes de conectar
        SubscribeToNetworkEvents();

        // Mostrar loading screen
        BeginConnection();

        if (ConfigureTransport(ipAddress, port))
        {
            // Obtener el transport para verificación final
            var transport = NetworkManager.singleton.GetComponent<KcpTransport>();
            LogInfo($"📡 Final transport configuration before StartClient:");
            LogInfo($"   → Address: {NetworkManager.singleton.networkAddress}");
            LogInfo($"   → Port: {transport.Port}");

            NetworkManager.singleton.StartClient();
            bool success = NetworkClient.isConnected || NetworkClient.isConnecting;

            if (success || NetworkClient.isConnecting)
            {
                LogInfo($"✅ Client connection initiated to {ipAddress}:{port}");
            }
            else
            {
                LogError("❌ Failed to start client!");
                HideLoadingScreen();
            }
        }
        else
        {
            LogError("❌ Failed to configure transport!");
            HideLoadingScreen();
        }
    }
    
    /// <summary>
    /// Configura el KCP Transport con la dirección IP y puerto del servidor
    /// </summary>
    private bool ConfigureTransport(string ipAddress, ushort port)
    {
        var transport = NetworkManager.singleton.GetComponent<KcpTransport>();

        if (transport == null)
        {
            LogError("KcpTransport component not found on NetworkManager!");
            return false;
        }

        // En Mirror, la dirección se configura en el NetworkManager, no en el transport
        NetworkManager.singleton.networkAddress = ipAddress;
        transport.Port = port;

        // Logging detallado para debugging
        LogInfo($"✅ KcpTransport configured:");
        LogInfo($"   → Address: {NetworkManager.singleton.networkAddress}");
        LogInfo($"   → Port: {transport.Port}");

        // Verificar que la configuración se aplicó correctamente
        if (NetworkManager.singleton.networkAddress != ipAddress)
        {
            LogError($"❌ Transport Address not set correctly! Expected: {ipAddress}, Got: {NetworkManager.singleton.networkAddress}");
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Llama esto antes de conectar al servidor para mostrar el loading screen
    /// </summary>
    public void BeginConnection()
    {
        LogInfo("BeginConnection() - Showing loading screen via events");
        isConnecting = true;
        
        // Usar el sistema de eventos en lugar de llamar al controller directamente
        LoadingScreenEvents.Show();
    }
    
    /// <summary>
    /// Oculta el loading screen manualmente
    /// </summary>
    public void HideLoadingScreen()
    {
        LogInfo("HideLoadingScreen() called - using events");
        
        LoadingScreenEvents.Hide();
        
        isConnecting = false;
    }
    
    #endregion
    
    #region Network Event Callbacks

    private void OnClientConnected()
    {
        LogInfo($"🎉 Local client connected to server!");
        LogInfo($"   IsClient: {NetworkClient.isConnected}");
        LogInfo($"   IsServer: {NetworkServer.active}");
    }

    private void OnClientDisconnected()
    {
        LogWarning($"❌ Local client disconnected from server");

        LoadingScreenEvents.Hide();

        isConnecting = false;
    }

    // Mirror uses Unity's SceneManager.sceneLoaded instead of network-specific scene events
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogInfo($"📍 OnSceneLoaded called: Scene={scene.name}, mode={mode}, isConnecting={isConnecting}");

        // Solo procesar si estamos conectando como cliente
        if (isConnecting && NetworkClient.isConnected)
        {
            LogInfo($"Scene '{scene.name}' loaded. Hiding loading screen with delay...");

            LoadingScreenEvents.HideWithDelay();

            isConnecting = false;
        }
    }

    #endregion
    
    #region Helper Methods
    
    private void LogInfo(string message)
    {
        if (enableLogging)
        {
            logger?.Log($"[NetworkConnectionHandler] {message}", this);
        }
    }
    
    private void LogWarning(string message)
    {
        if (enableLogging)
        {
            logger?.Log($"[NetworkConnectionHandler] {message}", this, Logging.LogType.Warning);
        }
    }
    
    private void LogError(string message)
    {
        if (enableLogging)
        {
            logger?.Log($"[NetworkConnectionHandler] {message}", this, Logging.LogType.Error);
        }
    }
    
    #endregion
}