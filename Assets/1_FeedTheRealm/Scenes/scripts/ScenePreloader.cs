using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Preloads game scenes in the background to improve loading times.
/// Attach this to a persistent GameObject in the menu scene.
/// </summary>
public class ScenePreloader : MonoBehaviour
{
    [System.Serializable]
    public class SceneReference
    {
#if UNITY_EDITOR
        [SerializeField] private Object sceneAsset;
#endif
        [SerializeField] private string sceneName;
        
        public string SceneName => sceneName;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sceneAsset != null)
            {
                sceneName = sceneAsset.name;
            }
        }
#endif
    }
    
    [Header("Preload Settings")]
    [SerializeField] private SceneReference[] scenesToPreload;
    [SerializeField] private bool preloadOnStart = true;
    [SerializeField] private float delayBeforePreload = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private Logging.Logger logger;
    
    private bool isPreloading = false;
    private bool preloadComplete = false;
    
    private void Start()
    {
        if (preloadOnStart)
        {
            StartCoroutine(PreloadScenesAfterDelay());
        }
    }
    
    private IEnumerator PreloadScenesAfterDelay()
    {
        // Esperar un poco para no sobrecargar el inicio
        yield return new WaitForSeconds(delayBeforePreload);
        
        yield return StartCoroutine(PreloadScenes());
    }
    
    private IEnumerator PreloadScenes()
    {
        if (isPreloading)
        {
            logger.Log("[ScenePreloader] Already preloading scenes", this, Logging.LogType.Warning);
            yield break;
        }
        
        isPreloading = true;
        logger.Log($"[ScenePreloader] Starting to preload {scenesToPreload.Length} scene(s)...", this, Logging.LogType.Info);
        
        foreach (SceneReference sceneRef in scenesToPreload)
        {
            if (sceneRef == null || string.IsNullOrEmpty(sceneRef.SceneName))
            {
                logger.Log("[ScenePreloader] Invalid scene reference, skipping", this, Logging.LogType.Warning);
                continue;
            }
            
            string sceneName = sceneRef.SceneName;
            
            // Verificar si la escena ya está cargada
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                logger.Log($"[ScenePreloader] Scene '{sceneName}' is already loaded, skipping", this, Logging.LogType.Info);
                continue;
            }
            
            logger.Log($"[ScenePreloader] Preloading scene '{sceneName}'...", this, Logging.LogType.Info);
            
            // Cargar la escena de forma aditiva en segundo plano
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            
            if (asyncLoad == null)
            {
                logger.Log($"[ScenePreloader] Failed to start loading scene '{sceneName}'", this, Logging.LogType.Error);
                continue;
            }
            
            // No activar la escena automáticamente
            asyncLoad.allowSceneActivation = false;
            
            // Esperar a que la carga llegue al 90% (Unity mantiene en 0.9 hasta que se active)
            while (asyncLoad.progress < 0.9f)
            {
                if (showDebugLogs)
                {
                    logger.Log($"[ScenePreloader] Loading '{sceneName}': {asyncLoad.progress * 100f:F1}%", this, Logging.LogType.Info);
                }
                yield return null;
            }
            
            logger.Log($"[ScenePreloader] Scene '{sceneName}' preloaded (ready to activate)", this, Logging.LogType.Info);
            
            // Mantener la referencia a la AsyncOperation para activar más tarde si es necesario
            // Por ahora, la dejamos lista pero no activada
        }
        
        isPreloading = false;
        preloadComplete = true;
        logger.Log("[ScenePreloader] All scenes preloaded successfully!", this, Logging.LogType.Info);
    }
    
    /// <summary>
    /// Manually trigger preload if not set to auto-preload
    /// </summary>
    public void StartPreload()
    {
        if (!isPreloading && !preloadComplete)
        {
            StartCoroutine(PreloadScenes());
        }
    }
    
    /// <summary>
    /// Check if preload is complete
    /// </summary>
    public bool IsPreloadComplete()
    {
        return preloadComplete;
    }
    
    /// <summary>
    /// Unload preloaded scenes (useful when returning to menu)
    /// </summary>
    public void UnloadPreloadedScenes()
    {
        StartCoroutine(UnloadScenes());
    }
    
    private IEnumerator UnloadScenes()
    {
        foreach (SceneReference sceneRef in scenesToPreload)
        {
            if (sceneRef == null || string.IsNullOrEmpty(sceneRef.SceneName))
            {
                continue;
            }
            
            string sceneName = sceneRef.SceneName;
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                logger.Log($"[ScenePreloader] Unloading scene '{sceneName}'...", this, Logging.LogType.Info);
                yield return SceneManager.UnloadSceneAsync(sceneName);
            }
        }
        
        preloadComplete = false;
        logger.Log("[ScenePreloader] Preloaded scenes unloaded", this, Logging.LogType.Info);
    }
}
