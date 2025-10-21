using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkSceneManager : NetworkBehaviour
{
    public static NetworkSceneManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadGameScene()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Cargando escena del juego...");
            // Asegúrate que este nombre coincida EXACTAMENTE con tu escena
            NetworkManager.SceneManager.LoadScene("MultiplayerScene", LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Solo el servidor puede cargar escenas!");
        }
    }

    [ServerRpc]
    public void LoadGameSceneServerRpc()
    {
        NetworkManager.SceneManager.LoadScene("MultiplayerScene", LoadSceneMode.Single);
    }

    public void DisconnectAndReturnToMenu() {
        Debug.Log("Desconectando y volviendo al menú...");

        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenuScene");
    }
    

        private void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} conectado. ¿Es servidor?: {NetworkManager.Singleton.IsServer}");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Escena cargada: {scene.name}. ¿Es servidor?: {NetworkManager.Singleton.IsServer}");
    } 
}