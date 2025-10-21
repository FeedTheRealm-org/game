using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    
    private void Start()
    {
        Debug.Log($"=== DEBUG START ===");
        Debug.Log($"Escena actual: {SceneManager.GetActiveScene().name}");
        Debug.Log($"NetworkManager: {NetworkManager.Singleton != null}");
        
        if (NetworkManager.Singleton != null)
        {
            Debug.Log($"IsServer: {NetworkManager.Singleton.IsServer}");
            Debug.Log($"IsClient: {NetworkManager.Singleton.IsClient}");
            Debug.Log($"IsHost: {NetworkManager.Singleton.IsHost}");
        }
        
        // Suscribirse a eventos
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🔁 ESCENA CARGADA: {scene.name}");
        Debug.Log($"NetworkManager exists: {NetworkManager.Singleton != null}");
        
        if (NetworkManager.Singleton != null)
        {
            Debug.Log($"Connection状态: Server={NetworkManager.Singleton.IsServer}, Client={NetworkManager.Singleton.IsClient}");
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"👤 Cliente conectado: {clientId}");
        Debug.Log($"Local ClientId: {NetworkManager.Singleton.LocalClientId}");
    }
    
    private void Update()
    {
        // Log cada 5 segundos para debugging continuo
        if (Time.frameCount % 300 == 0)
        {
            Debug.Log($"🕐 Frame: {Time.frameCount}, Escena: {SceneManager.GetActiveScene().name}");
        }
    }
}