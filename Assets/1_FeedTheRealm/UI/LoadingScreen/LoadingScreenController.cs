using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Controla la visibilidad y animación del loading screen usando UI Toolkit.
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument loadingScreenDocument;
    
    [Header("Configuración")]
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private bool hideOnStart = true;
    
    [Header("Debug")]
    [SerializeField] private Logging.Logger logger;
    
    private VisualElement rootElement;
    private static LoadingScreenController instance;
    private Coroutine activeCoroutine;
    
    public static LoadingScreenController Instance => instance;
    
    private void Awake()
    {
        // Singleton pattern para acceder desde cualquier parte
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            logger?.Log("LoadingScreenController: Singleton creado", this, Logging.LogType.Info);
        }
        else
        {
            logger?.Log("LoadingScreenController: Instancia duplicada, destruyendo", this, Logging.LogType.Warning);
            Destroy(gameObject);
            return;
        }
        
        // Inicializar UI
        if (loadingScreenDocument == null)
        {
            loadingScreenDocument = GetComponent<UIDocument>();
            logger?.Log($"UIDocument autodetectado: {loadingScreenDocument != null}", this, Logging.LogType.Info);
        }
        
        if (loadingScreenDocument != null)
        {
            rootElement = loadingScreenDocument.rootVisualElement;
            logger?.Log($"rootVisualElement obtenido - es null: {rootElement == null}", this, Logging.LogType.Info);
            
            // Solo ocultar si hideOnStart está activado
            if (hideOnStart)
            {
                Hide();
            }
            else
            {
                logger?.Log("hideOnStart está desactivado, NO se oculta el loading screen", this, Logging.LogType.Info);
            }
        }
        else
        {
            logger?.Log("UIDocument no encontrado en LoadingScreenController", this, Logging.LogType.Error);
        }
    }
    
    /// <summary>
    /// Muestra el loading screen inmediatamente.
    /// </summary>
    public void Show()
    {
        logger?.Log($"Show() llamado - rootElement is null: {rootElement == null}", this, Logging.LogType.Info);
        
        if (rootElement != null)
        {
            rootElement.style.display = DisplayStyle.Flex;
            rootElement.style.opacity = 1f;
            logger?.Log("Loading screen mostrado exitosamente", this, Logging.LogType.Info);
        }
        else
        {
            logger?.Log("ERROR: rootElement es null, no se puede mostrar loading screen", this, Logging.LogType.Error);
            
            // Intentar reinicializar
            if (loadingScreenDocument != null)
            {
                rootElement = loadingScreenDocument.rootVisualElement;
                logger?.Log($"Reintento de inicialización - rootElement ahora es null: {rootElement == null}", this, Logging.LogType.Info);
                
                if (rootElement != null)
                {
                    rootElement.style.display = DisplayStyle.Flex;
                    rootElement.style.opacity = 1f;
                    logger?.Log("Loading screen mostrado después de reinicialización", this, Logging.LogType.Info);
                }
            }
        }
    }
    
    /// <summary>
    /// Oculta el loading screen inmediatamente.
    /// </summary>
    public void Hide()
    {
        if (rootElement != null)
        {
            rootElement.style.display = DisplayStyle.None;
            rootElement.style.opacity = 0f;
            logger?.Log("Loading screen ocultado", this, Logging.LogType.Info);
        }
    }
    
    /// <summary>
    /// Muestra el loading screen, espera la duración especificada y luego lo desvanece.
    /// </summary>
    public void ShowWithDuration()
    {
        // Cancelar cualquier coroutine anterior
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        
        activeCoroutine = StartCoroutine(ShowWithDurationCoroutine());
    }
    
    /// <summary>
    /// Muestra el loading screen de forma asíncrona usando un callback cuando termine.
    /// </summary>
    public void ShowWithDuration(System.Action onComplete)
    {
        // Cancelar cualquier coroutine anterior
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        
        activeCoroutine = StartCoroutine(ShowWithDurationCoroutine(onComplete));
    }
    
    /// <summary>
    /// Oculta el loading screen después de esperar la duración especificada y hacer fade out.
    /// Útil cuando el loading screen ya está visible.
    /// </summary>
    public void HideWithDelay()
    {
        // Cancelar cualquier coroutine anterior
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        
        activeCoroutine = StartCoroutine(HideWithDelayCoroutine());
    }
    
    private IEnumerator HideWithDelayCoroutine()
    {
        logger?.Log($"HideWithDelay iniciado - esperando {displayDuration}s", this, Logging.LogType.Info);
        
        // Esperar la duración especificada
        yield return new WaitForSeconds(displayDuration);
        
        logger?.Log("Iniciando fade out", this, Logging.LogType.Info);
        
        // Fade out
        yield return StartCoroutine(FadeOut());
        
        activeCoroutine = null;
    }
    
    private IEnumerator ShowWithDurationCoroutine(System.Action onComplete = null)
    {
        Show();
        
        // Esperar la duración especificada
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out
        yield return StartCoroutine(FadeOut());
        
        // Llamar al callback si existe
        onComplete?.Invoke();
        
        activeCoroutine = null;
    }
    
    private IEnumerator FadeOut()
    {
        if (rootElement == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            rootElement.style.opacity = alpha;
            yield return null;
        }
        
        // Asegurar que esté completamente oculto
        Hide();
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            // Detener cualquier coroutine activa
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
            
            instance = null;
        }
    }
}
