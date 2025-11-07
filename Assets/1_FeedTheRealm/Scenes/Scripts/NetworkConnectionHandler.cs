using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
        if (NetworkManager.Singleton == null)
        {
            LogError("Cannot subscribe to network events: NetworkManager.Singleton is null!");
            return;
        }
        
        if (isSubscribedToEvents)
        {
            LogInfo("Already subscribed to network events, skipping...");
            return;
        }
        
        LogInfo("Subscribing to Network events...");
        
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        TrySubscribeToSceneEvents();
        
        isSubscribedToEvents = true;
    }
    
    private void TrySubscribeToSceneEvents()
    {
        if (isSubscribedToSceneEvents)
        {
            return;
        }
        
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null)
        {
            LogInfo("SceneManager not ready yet, will subscribe after client starts");
            return;
        }
        
        LogInfo("Subscribing to SceneManager events...");
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoadCompleted;
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
        isSubscribedToSceneEvents = true;
        LogInfo("✅ Subscribed to SceneManager events successfully");
    }
    

    private void UnsubscribeFromNetworkEvents()
    {
        if (!isSubscribedToEvents)
        {
            return;
        }
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            
            if (isSubscribedToSceneEvents && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnNetworkSceneLoadCompleted;
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
                isSubscribedToSceneEvents = false;
            }
        }
        
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
        if (NetworkManager.Singleton == null)
        {
            LogError("NetworkManager.Singleton is null!");
            return;
        }

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            LogWarning("Already connected or connecting!");
            return;
        }

        LogInfo($"Attempting to connect to server at {ipAddress}:{port}");

        // Suscribirse a eventos de red justo antes de conectar
        SubscribeToNetworkEvents();

        // Mostrar loading screen
        BeginConnection();

        if (ConfigureTransport(ipAddress, port))
        {
            bool success = NetworkManager.Singleton.StartClient();
            
            if (success)
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
            HideLoadingScreen();
        }
    }
    
    /// <summary>
    /// Configura el Unity Transport con la dirección IP y puerto del servidor
    /// </summary>
    private bool ConfigureTransport(string ipAddress, ushort port)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        
        if (transport == null)
        {
            LogError("UnityTransport component not found on NetworkManager!");
            return false;
        }

        transport.ConnectionData.Address = ipAddress;
        transport.ConnectionData.Port = port;
        
        LogInfo($"UnityTransport configured: {ipAddress}:{port}");
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
    
    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            LogInfo($"Local client {clientId} connected to server");
            
            TrySubscribeToSceneEvents();
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        LogInfo($"🔌 OnClientDisconnected called: clientId={clientId}, LocalClientId={NetworkManager.Singleton?.LocalClientId}");
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId)
        {
            LogWarning($"❌ Local client {clientId} disconnected from server");
            
            LoadingScreenEvents.Hide();
            
            isConnecting = false;
        }
    }
    
    private void OnNetworkSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, 
        System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        LogInfo($"📍 OnNetworkSceneLoadCompleted called: Scene={sceneName}, isConnecting={isConnecting}, IsClient={NetworkManager.Singleton?.IsClient}, IsServer={NetworkManager.Singleton?.IsServer}");
        
        // Solo procesar si estamos conectando como cliente y no somos servidor
        if (isConnecting && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            LogInfo($"Network scene '{sceneName}' load completed. Hiding loading screen with delay via events...");
            
            LoadingScreenEvents.HideWithDelay();
            
            isConnecting = false;
        }
        else
        {
            LogInfo($"Skipping loading screen hide: isConnecting={isConnecting}, IsClient={NetworkManager.Singleton?.IsClient}, IsServer={NetworkManager.Singleton?.IsServer}");
        }
    }
    
    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            LogInfo($"Scene Event: {sceneEvent.SceneEventType} - Scene: {sceneEvent.SceneName}");
            
            if (sceneEvent.SceneEventType == SceneEventType.Load && isConnecting)
            {
                LogInfo("Scene loading started - ensuring loading screen is visible via events");
                LoadingScreenEvents.Show();
            }
            
            if (sceneEvent.SceneEventType == SceneEventType.LoadComplete && isConnecting)
            {
                LogInfo($"✅ Scene '{sceneEvent.SceneName}' load complete detected! Hiding loading screen with delay via events...");
                
                LoadingScreenEvents.HideWithDelay();
                
                isConnecting = false;
            }
            
            if (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete && isConnecting)
            {
                LogInfo($"✅ Scene synchronization complete! Hiding loading screen with delay via events...");
                
                // Usar el sistema de eventos para ocultar el loading screen
                LoadingScreenEvents.HideWithDelay();
                
                isConnecting = false;
            }
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