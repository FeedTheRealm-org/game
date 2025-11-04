using UnityEngine;

public class MenuSceneManager : MonoBehaviour
{
    [Header("Network Connection Handler")]
    [SerializeField] private NetworkConnectionHandler networkConnectionHandler;
    
    private void Awake()
    {
        if (networkConnectionHandler == null)
        {
            networkConnectionHandler = FindFirstObjectByType<NetworkConnectionHandler>();
            Debug.Log($"[MenuSceneManager] NetworkConnectionHandler auto-detected: {networkConnectionHandler != null}");
        }
    }
    
    public void ConnectToServer()
    {
        if (networkConnectionHandler == null)
        {
            Debug.LogError("[MenuSceneManager] NetworkConnectionHandler is not assigned!");
            return;
        }
        
        Debug.Log("[MenuSceneManager] ConnectToServer() called - delegating to NetworkConnectionHandler");
        networkConnectionHandler.ConnectToServer();
    }
    
    public void QuitGame() => Application.Quit();
}
