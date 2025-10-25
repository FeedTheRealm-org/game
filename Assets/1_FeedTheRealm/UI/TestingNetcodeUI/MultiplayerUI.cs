using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Logging;

public class SimpleMultiplayerMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button clientButton;
    public TextMeshProUGUI statusText;
    public GameObject menuPanel;
    public GameObject loadingPanel;

    [Header("Scene Settings")]
    public string gameSceneName = "MultiplayerScene";
    
    [Header("Loading Screen")]
    [SerializeField] private LoadingScreenController loadingScreenController;
    
    [Header("Preload")]
    public ScenePreloader scenePreloader; // Referencia al preloader

    [Header("Debug")]
    [SerializeField] private Logging.Logger logger;

    private void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        
        // Buscar LoadingScreenController si no está asignado
        if (loadingScreenController == null)
        {
            loadingScreenController = FindFirstObjectByType<LoadingScreenController>();
            logger?.Log($"LoadingScreenController auto-detected: {loadingScreenController != null}", this);
        }
        
        // Ocultar panel de loading al inicio
        if (loadingPanel != null) loadingPanel.SetActive(false);
        
        UpdateStatus("Menu ready - Choose an option");
        
        // Suscribirse a eventos de conexión
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        // Iniciar precarga si el preloader está asignado
        if (scenePreloader != null && !scenePreloader.IsPreloadComplete())
        {
            UpdateStatus("Preloading game scene...");
            scenePreloader.StartPreload();
            StartCoroutine(WaitForPreloadAndUpdateStatus());
        }
    }
    
    private System.Collections.IEnumerator WaitForPreloadAndUpdateStatus()
    {
        // Esperar a que termine la precarga
        while (!scenePreloader.IsPreloadComplete())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        UpdateStatus("✅ Ready to play - Choose an option");
    }

    public void StartHost()
    {
        UpdateStatus("Starting as Host...");
        ShowLoadingPanel();
        
        // Mostrar loading screen
        if (loadingScreenController != null)
        {
            logger?.Log("[MultiplayerUI] Showing loading screen for Host", this);
            loadingScreenController.Show();
        }
        else
        {
            logger?.Log("[MultiplayerUI] LoadingScreenController not found!", this, Logging.LogType.Warning);
        }
        
        StartCoroutine(StartHostCoroutine());
    }
    
    private System.Collections.IEnumerator StartHostCoroutine()
    {
        // Esperar un frame para que el loading screen se renderice
        yield return null;
        
        if (NetworkManager.Singleton.StartHost())
        {
            logger.Log("Host started - Switching to game scene", this);
            
            // Esperar un poco antes de cambiar escena
            yield return new WaitForSeconds(0.5f);
            
            // Cambiar a escena del juego
            LoadGameScene();
            
            // Iniciar el ocultamiento automático del loading screen después del delay
            // (que tiene DontDestroyOnLoad y sobrevivirá al cambio de escena)
            if (loadingScreenController != null)
            {
                logger?.Log("[MultiplayerUI] Starting auto-hide of loading screen with delay", this);
                loadingScreenController.HideWithDelay();
            }
        }
        else
        {
            UpdateStatus("❌ Error starting Host");
            HideLoadingPanel();
            
            // Ocultar loading screen si hay error
            if (loadingScreenController != null)
            {
                loadingScreenController.Hide();
            }
        }
    }

    public void StartClient()
    {
        UpdateStatus("Connecting as Client...");
        ShowLoadingPanel();
        
        // Mostrar loading screen
        if (loadingScreenController != null)
        {
            logger?.Log("[MultiplayerUI] Showing loading screen for Client", this);
            loadingScreenController.Show();
        }
        
        StartCoroutine(StartClientCoroutine());
    }
    
    private System.Collections.IEnumerator StartClientCoroutine()
    {
        // Esperar un frame para que el loading screen se renderice
        yield return null;
        
        if (NetworkManager.Singleton.StartClient())
        {
            logger.Log("Client connected - Waiting for host scene...", this);
            // El cliente esperará a que el host cargue la escena
            
            // Iniciar el ocultamiento automático del loading screen después del delay
            if (loadingScreenController != null)
            {
                logger?.Log("[MultiplayerUI] Starting auto-hide of loading screen with delay for client", this);
                loadingScreenController.HideWithDelay();
            }
        }
        else
        {
            UpdateStatus("❌ Error connecting");
            HideLoadingPanel();
            
            // Ocultar loading screen si hay error
            if (loadingScreenController != null)
            {
                loadingScreenController.Hide();
            }
        }
    }

    private void LoadGameScene()
    {
        if (NetworkManager.Singleton.SceneManager != null)
        {
            logger.Log($"[MultiplayerUI] Loading game scene '{gameSceneName}'...", this);
            
            // Si la escena está precargada, descargarla primero y luego cargarla con NetworkSceneManager
            Scene preloadedScene = SceneManager.GetSceneByName(gameSceneName);
            if (preloadedScene.isLoaded)
            {
                logger.Log($"[MultiplayerUI] Scene was preloaded, unloading it first...", this);
                StartCoroutine(UnloadPreloadedThenLoadNetwork());
            }
            else
            {
                // Cargar directamente con NetworkSceneManager
                LoadNetworkScene();
            }
        }
        else
        {
            logger.Log("NetworkSceneManager no encontrado", this, Logging.LogType.Error);
            // Fallback: cargar escena normalmente
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        }
    }
    
    private System.Collections.IEnumerator UnloadPreloadedThenLoadNetwork()
    {
        // Descargar la escena precargada
        yield return SceneManager.UnloadSceneAsync(gameSceneName);
        
        logger.Log($"[MultiplayerUI] Preloaded scene unloaded, now loading via NetworkSceneManager...", this);
        
        // Ahora cargar con NetworkSceneManager
        LoadNetworkScene();
    }
    
    private void LoadNetworkScene()
    {
        // Usar NetworkSceneManager para cambiar escena
        var status = NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName,
            LoadSceneMode.Single
        );
        logger.Log("Scene load status: " + status, this);
        
        if (status != SceneEventProgressStatus.Started)
        {
            logger.Log("Error loading scene: " + status, this, Logging.LogType.Error);
            UpdateStatus("❌ Error loading game scene");
            HideLoadingPanel();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        logger.Log($"Client connected: {clientId}", this);
        
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                UpdateStatus("✅ Host running - Loading game...");
            }
            else
            {
                UpdateStatus("✅ Connected to server - Waiting for game...");
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            UpdateStatus("❌ Disconnected from server");
            HideLoadingPanel();
            ShowMenuPanel();
        }
    }

    // Manejar evento cuando la escena se carga via Network
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoaded;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnNetworkSceneLoaded;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnNetworkSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        logger.Log($"Scene loaded via network: {sceneName}", this);
        if (sceneName == gameSceneName)
        {
            // Escena del juego cargada - ocultar UI completamente
            HideAllUI();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        logger.Log($"Scene loaded: {scene.name}", this);
        if (scene.name == gameSceneName)
        {
            HideAllUI();
        }
    }

    private void ShowLoadingPanel()
    {
        menuPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(true);
    }

    private void HideLoadingPanel()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    private void ShowMenuPanel()
    {
        menuPanel.SetActive(true);
        HideLoadingPanel();
    }

    private void HideAllUI()
    {
        menuPanel.SetActive(false);
        HideLoadingPanel();
                    UpdateStatus("Game in progress...");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        logger.Log("Multiplayer Status: " + message, this);
    }
}