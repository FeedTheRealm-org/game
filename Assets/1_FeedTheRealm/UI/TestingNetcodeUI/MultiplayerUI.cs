using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

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
    }

    public void StartHost()
    {
        UpdateStatus("Iniciando como Host...");
        ShowLoadingPanel();
        
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host iniciado - Cambiando a escena del juego");
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
            Debug.Log("Cliente conectado - Esperando escena del host...");
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
            // Usar NetworkSceneManager para cambiar escena
            var status = NetworkManager.Singleton.SceneManager.LoadScene(
                gameSceneName,
                LoadSceneMode.Single
            );
            Debug.Log("Estado carga de escena: " + status);
            
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError("Error al cargar la escena: " + status);
                UpdateStatus("❌ Error al cargar escena del juego");
                HideLoadingPanel();
            }
        }
        else
        {
            Debug.LogError("NetworkSceneManager no encontrado");
            // Fallback: cargar escena normalmente
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente conectado: {clientId}");
        
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
        Debug.Log($"Escena cargada via network: {sceneName}");
        if (sceneName == gameSceneName)
        {
            // Escena del juego cargada - ocultar UI completamente
            HideAllUI();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Escena cargada: {scene.name}");
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
        Debug.Log("Multiplayer Status: " + message);
    }
}