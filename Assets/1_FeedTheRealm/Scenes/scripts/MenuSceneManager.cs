using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UIElements;

public class MenuSceneManager : MonoBehaviour
{
    [Header("Loading Screen")]
    [SerializeField] private LoadingScreenController loadingScreenController;
    
    private Coroutine loadSceneCoroutine;
    
    private void Awake()
    {
        // Si no está asignado, intentar encontrarlo
        if (loadingScreenController == null)
        {
            loadingScreenController = FindFirstObjectByType<LoadingScreenController>();
            Debug.Log($"[MenuSceneManager] LoadingScreenController auto-detected: {loadingScreenController != null}");
        }
        else
        {
            Debug.Log($"[MenuSceneManager] LoadingScreenController already assigned: {loadingScreenController != null}");
        }
    }
    
    public void StartHost()
    {
        Debug.Log("[MenuSceneManager] StartHost() called");
        loadSceneCoroutine = StartCoroutine(StartHostCoroutine());
    }
    
    private System.Collections.IEnumerator StartHostCoroutine()
    {
        Debug.Log("[MenuSceneManager] StartHostCoroutine started");
        
        // Mostrar loading screen primero
        if (loadingScreenController != null)
        {
            Debug.Log("[MenuSceneManager] Calling loadingScreenController.Show()");
            loadingScreenController.Show();
        }
        else
        {
            Debug.LogError("[MenuSceneManager] loadingScreenController is NULL!");
        }
        
        // Esperar un frame para que el UI se renderice
        yield return null;
        
        // Iniciar host
        NetworkManager.Singleton.StartHost();
        
        // Esperar a que el host esté listo
        yield return new WaitForSeconds(0.5f);
        
        // Cargar la escena del juego
        NetworkSceneManager.Instance.LoadGameScene();
        
        // Esperar displayDuration y luego hacer fade out
        yield return new WaitForSeconds(2f);
        
        // Iniciar fade out
        if (loadingScreenController != null)
        {
            yield return StartCoroutine(FadeOutLoadingScreen());
        }
        
        loadSceneCoroutine = null;
    }
    
    private System.Collections.IEnumerator FadeOutLoadingScreen()
    {
        float fadeOutDuration = 1f;
        var uiDocument = loadingScreenController.GetComponent<UIDocument>();
        
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            var rootElement = uiDocument.rootVisualElement;
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                rootElement.style.opacity = alpha;
                yield return null;
            }
            
            rootElement.style.display = DisplayStyle.None;
        }
    }

    public void StartClient()
    {
        Debug.Log("[MenuSceneManager] StartClient() called");
        loadSceneCoroutine = StartCoroutine(StartClientCoroutine());
    }
    
    private System.Collections.IEnumerator StartClientCoroutine()
    {
        Debug.Log("[MenuSceneManager] StartClientCoroutine started");
        
        // Mostrar loading screen primero
        if (loadingScreenController != null)
        {
            Debug.Log("[MenuSceneManager] Calling loadingScreenController.Show() for client");
            loadingScreenController.Show();
        }
        else
        {
            Debug.LogError("[MenuSceneManager] loadingScreenController is NULL in StartClient!");
        }
        
        // Esperar un frame para que el UI se renderice
        yield return null;
        
        // Iniciar cliente
        NetworkManager.Singleton.StartClient();
        
        // Esperar displayDuration
        yield return new WaitForSeconds(2f);
        
        // Iniciar fade out
        if (loadingScreenController != null)
        {
            yield return StartCoroutine(FadeOutLoadingScreen());
        }
        
        loadSceneCoroutine = null;
    }

    public void QuitGame() => Application.Quit();
    
    private void OnDestroy()
    {
        if (loadSceneCoroutine != null)
        {
            StopCoroutine(loadSceneCoroutine);
            loadSceneCoroutine = null;
        }
    }
}