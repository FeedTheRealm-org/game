using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneVerifier : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== VERIFICACIÓN DE ESCENAS EN BUILD ===");
        
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        Debug.Log($"Número de escenas en Build Settings: {sceneCount}");
        
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Debug.Log($"Escena {i}: {sceneName} (Path: {scenePath})");
        }
        
        Debug.Log($"Escena actual: {SceneManager.GetActiveScene().name}");
    }
}