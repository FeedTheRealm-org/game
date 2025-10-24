using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkSceneManager : NetworkBehaviour
{
    public static NetworkSceneManager Instance;

    //[SerializeField] private Logging.Logger logger;


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
            //logger.Log("Cargando escena del juego...", this, Logging.LogType.Info);
            // Asegúrate que este nombre coincida EXACTAMENTE con tu escena
            NetworkManager.SceneManager.LoadScene("MultiplayerScene", LoadSceneMode.Single);
        }
        else
        {
            //logger.Log("Solo el servidor puede cargar escenas!", this, Logging.LogType.Error);
        }
    }

    [ServerRpc]
    public void LoadGameSceneServerRpc()
    {
        NetworkManager.SceneManager.LoadScene("MultiplayerScene", LoadSceneMode.Single);
    }

    public void DisconnectAndReturnToMenu()
    {
        //logger.Log("Desconectando y volviendo al menú...", this, Logging.LogType.Info);

        if (NetworkManager.Singleton != null)
        {
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
        //logger.Log($"Cliente {clientId} conectado. ¿Es servidor?: {NetworkManager.Singleton.IsServer}", this, Logging.LogType.Info);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //logger.Log($"Escena cargada: {scene.name}. ¿Es servidor?: {NetworkManager.Singleton.IsServer}", this, Logging.LogType.Info);
    } 
}