using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Maneja el ciclo de vida de la conexión de red y coordina el loading screen.
/// Este componente persiste entre cambios de escena usando DontDestroyOnLoad.
/// También maneja la configuración de IP y puerto para conectar al servidor.
/// </summary>
public class NetworkConnectionHandler : MonoBehaviour
{
    private static NetworkConnectionHandler instance;
    public static NetworkConnectionHandler Instance => instance;
    
    [Header("Connection Settings")]
    [SerializeField] private string defaultIP = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7777;
    
    [Header("UI References (Optional)")]
    [Tooltip("Input field para la IP del servidor. Si está vacío, usa defaultIP")]
    [SerializeField] private TMP_InputField ipInputField;
    
    [Tooltip("Input field para el puerto del servidor. Si está vacío, usa defaultPort")]
    [SerializeField] private TMP_InputField portInputField;
    
    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;
    
    private bool isConnecting = false;
    
    private void Awake()
    {
        // Singleton pattern con DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            LogInfo($"⚠️ DUPLICATE INSTANCE DETECTED! Destroying this instance. Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        LogInfo($"NetworkConnectionHandler initialized with DontDestroyOnLoad in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        
        // Inicializar campos de input con valores por defecto
        if (ipInputField != null)
        {
            ipInputField.text = defaultIP;
            LogInfo($"IP Input field initialized with: {defaultIP}");
        }
        
        if (portInputField != null)
        {
            portInputField.text = defaultPort.ToString();
            LogInfo($"Port Input field initialized with: {defaultPort}");
        }
        
        // Suscribirse a eventos de Netcode
        SubscribeToNetworkEvents();
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
        // Esperar a que NetworkManager esté listo
        if (NetworkManager.Singleton == null)
        {
            LogInfo("⚠️ NetworkManager.Singleton is null, deferring subscription");
            StartCoroutine(WaitForNetworkManagerAndSubscribe());
            return;
        }
        
        LogInfo("Subscribing to Network events...");
        
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoadCompleted;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            LogInfo("✅ Subscribed to SceneManager events");
        }
        else
        {
            LogInfo("⚠️ SceneManager is null, will retry subscription");
            StartCoroutine(WaitForSceneManagerAndSubscribe());
        }
    }
    
    private System.Collections.IEnumerator WaitForNetworkManagerAndSubscribe()
    {
        LogInfo("Waiting for NetworkManager.Singleton...");
        while (NetworkManager.Singleton == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        LogInfo("NetworkManager.Singleton found, subscribing...");
        SubscribeToNetworkEvents();
    }
    
    private System.Collections.IEnumerator WaitForSceneManagerAndSubscribe()
    {
        LogInfo("Waiting for NetworkManager.SceneManager...");
        int attempts = 0;
        while (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager == null && attempts < 50)
        {
            attempts++;
            yield return new WaitForSeconds(0.1f);
        }
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            LogInfo("SceneManager found, subscribing to scene events...");
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoadCompleted;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            LogInfo("✅ Subscribed to SceneManager events");
        }
        else
        {
            Debug.LogError("[NetworkConnectionHandler] Failed to subscribe to SceneManager events after 5 seconds");
        }
    }
    
    private void UnsubscribeFromNetworkEvents()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnNetworkSceneLoadCompleted;
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
        }
    }
    
    #region Public Methods
    
    /// <summary>
    /// Conecta al servidor usando los valores de los input fields o valores por defecto
    /// </summary>
    public void ConnectToServer()
    {
        string ip = GetIPAddress();
        ushort port = GetPort();
        
        ConnectToServer(ip, port);
    }
    
    /// <summary>
    /// Conecta al servidor con IP y puerto específicos
    /// </summary>
    public void ConnectToServer(string ipAddress, ushort port)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkConnectionHandler] NetworkManager.Singleton is null!");
            return;
        }

        // Verificar que no estamos ya conectados
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("[NetworkConnectionHandler] Already connected or connecting!");
            return;
        }

        LogInfo($"Attempting to connect to server at {ipAddress}:{port}");

        // Mostrar loading screen
        BeginConnection();

        // Configurar el transporte con la IP y puerto del servidor
        if (ConfigureTransport(ipAddress, port))
        {
            // Iniciar el cliente
            bool success = NetworkManager.Singleton.StartClient();
            
            if (success)
            {
                LogInfo($"✅ Client connection initiated to {ipAddress}:{port}");
            }
            else
            {
                Debug.LogError($"[NetworkConnectionHandler] ❌ Failed to start client!");
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
            Debug.LogError("[NetworkConnectionHandler] UnityTransport component not found on NetworkManager!");
            return false;
        }

        // Configurar los datos de conexión
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
        
        // Usar el sistema de eventos
        LoadingScreenEvents.Hide();
        
        isConnecting = false;
    }
    
    #endregion
    
    #region Network Event Callbacks
    
    private void OnClientConnected(ulong clientId)
    {
        // Solo procesar para el cliente local
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            LogInfo($"Local client {clientId} connected to server");
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        LogInfo($"🔌 OnClientDisconnected called: clientId={clientId}, LocalClientId={NetworkManager.Singleton?.LocalClientId}");
        
        // Solo procesar para el cliente local
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId)
        {
            LogInfo($"❌ Local client {clientId} disconnected from server - Stack trace:");
            Debug.LogWarning($"[NetworkConnectionHandler] Disconnect stack trace:\n{System.Environment.StackTrace}");
            
            // Ocultar loading screen si está visible usando eventos
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
            
            // Usar el sistema de eventos para ocultar el loading screen
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
        // Logging de eventos de escena para debugging
        if (verboseLogging && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            LogInfo($"Scene Event: {sceneEvent.SceneEventType} - Scene: {sceneEvent.SceneName}");
            
            // Cuando empieza la carga de escena, asegurar que el loading screen está visible
            if (sceneEvent.SceneEventType == SceneEventType.Load && isConnecting)
            {
                LogInfo("Scene loading started - ensuring loading screen is visible via events");
                LoadingScreenEvents.Show();
            }
            
            // ✅ Ocultar loading screen cuando la carga está completa
            if (sceneEvent.SceneEventType == SceneEventType.LoadComplete && isConnecting)
            {
                LogInfo($"✅ Scene '{sceneEvent.SceneName}' load complete detected! Hiding loading screen with delay via events...");
                
                // Usar el sistema de eventos para ocultar el loading screen
                LoadingScreenEvents.HideWithDelay();
                
                isConnecting = false;
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Obtiene la dirección IP del input field o usa el valor por defecto
    /// </summary>
    private string GetIPAddress()
    {
        if (ipInputField != null && !string.IsNullOrWhiteSpace(ipInputField.text))
        {
            return ipInputField.text.Trim();
        }
        
        return defaultIP;
    }

    /// <summary>
    /// Obtiene el puerto del input field o usa el valor por defecto
    /// </summary>
    private ushort GetPort()
    {
        if (portInputField != null && !string.IsNullOrWhiteSpace(portInputField.text))
        {
            if (ushort.TryParse(portInputField.text, out ushort port))
            {
                return port;
            }
            else
            {
                Debug.LogWarning($"[NetworkConnectionHandler] Invalid port in input field: {portInputField.text}. Using default: {defaultPort}");
            }
        }
        
        return defaultPort;
    }
    
    private void LogInfo(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[NetworkConnectionHandler] {message}");
        }
    }
    
    #endregion
}