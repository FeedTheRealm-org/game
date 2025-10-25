using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MenuSceneManager : MonoBehaviour
{
    private Coroutine loadSceneCoroutine;
    
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        // Esperar un frame para que el host esté listo antes de cambiar escena
        loadSceneCoroutine = StartCoroutine(LoadGameSceneAfterHost());
    }

    private System.Collections.IEnumerator LoadGameSceneAfterHost()
    {
        yield return new WaitForSeconds(0.5f); // Pequeño delay
        NetworkSceneManager.Instance.LoadGameScene();
        loadSceneCoroutine = null;
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        // Los clientes se unen automáticamente a la escena del host
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