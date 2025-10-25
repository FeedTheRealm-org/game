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
    
    [Header("Preload")]
    public ScenePreloader scenePreloader; // Referencia al preloader

    [Header("Debug")]
    [SerializeField] private Logging.Logger logger;

    private void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        
        // Ocultar panel de loading al inicio
        if (loadingPanel != null) loadingPanel.SetActive(false);
        
        UpdateStatus("Menú listo - Elige una opción");
        
        // Suscribirse a eventos de conexión
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        // Iniciar precarga si el preloader está asignado
        if (scenePreloader != null && !scenePreloader.IsPreloadComplete())
        {
            UpdateStatus("Precargando escena del juego...");
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
        
        UpdateStatus("✅ Listo para jugar - Elige una opción");
    }

    public void StartHost()
    {
        UpdateStatus("Iniciando como Host...");
        ShowLoadingPanel();
        
        if (NetworkManager.Singleton.StartHost())
        {
            logger.Log("Host iniciado - Cambiando a escena del juego", this, Logging.LogType.Info);
            // Cambiar a escena del juego
            LoadGameScene();
        }
        else
        {
            UpdateStatus("❌ Error al iniciar Host");
            HideLoadingPanel();
        }
    }

    public void StartClient()
    {
        UpdateStatus("Conectando como Cliente...");
        ShowLoadingPanel();
        
        if (NetworkManager.Singleton.StartClient())
        {
            logger.Log("Cliente conectado - Esperando escena del host...", this, Logging.LogType.Info);
            // El cliente esperará a que el host cargue la escena
        }
        else
        {
            UpdateStatus("❌ Error al conectar");
            HideLoadingPanel();
        }
    }

    private void LoadGameScene()
    {
        if (NetworkManager.Singleton.SceneManager != null)
        {
            logger.Log($"[MultiplayerUI] Loading game scene '{gameSceneName}'...", this, Logging.LogType.Info);
            
            // Si la escena está precargada, descargarla primero y luego cargarla con NetworkSceneManager
            Scene preloadedScene = SceneManager.GetSceneByName(gameSceneName);
            if (preloadedScene.isLoaded)
            {
                logger.Log($"[MultiplayerUI] Scene was preloaded, unloading it first...", this, Logging.LogType.Info);
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
        
        logger.Log($"[MultiplayerUI] Preloaded scene unloaded, now loading via NetworkSceneManager...", this, Logging.LogType.Info);
        
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
        logger.Log("Estado carga de escena: " + status, this, Logging.LogType.Info);
        
        if (status != SceneEventProgressStatus.Started)
        {
            logger.Log("Error al cargar la escena: " + status, this, Logging.LogType.Error);
            UpdateStatus("❌ Error al cargar escena del juego");
            HideLoadingPanel();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        logger.Log($"Cliente conectado: {clientId}", this, Logging.LogType.Info);
        
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                UpdateStatus("✅ Host ejecutándose - Cargando juego...");
            }
            else
            {
                UpdateStatus("✅ Conectado al servidor - Esperando juego...");
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            UpdateStatus("❌ Desconectado del servidor");
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
        logger.Log($"Escena cargada via network: {sceneName}", this, Logging.LogType.Info);
        if (sceneName == gameSceneName)
        {
            // Escena del juego cargada - ocultar UI completamente
            HideAllUI();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        logger.Log($"Escena cargada: {scene.name}", this, Logging.LogType.Info);
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
        UpdateStatus("Juego en progreso...");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        logger.Log("Multiplayer Status: " + message, this, Logging.LogType.Info);
    }
}