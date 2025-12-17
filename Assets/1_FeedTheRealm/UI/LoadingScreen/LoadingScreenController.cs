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
            logger?.Log("LoadingScreenController: Singleton created", this);
        }
        else
        {
            logger?.Log("LoadingScreenController: Duplicate instance, destroying", this, Logging.LogType.Warning);
            Destroy(gameObject);
            return;
        }
        
        // Inicializar UI
        if (loadingScreenDocument == null)
        {
            loadingScreenDocument = GetComponent<UIDocument>();
            logger?.Log($"UIDocument auto-detected: {loadingScreenDocument != null}", this);
        }
        
        if (loadingScreenDocument != null)
        {
            rootElement = loadingScreenDocument.rootVisualElement;
            logger?.Log($"rootVisualElement obtained - is null: {rootElement == null}", this);
            
            // Solo ocultar si hideOnStart está activado
            if (hideOnStart)
            {
                Hide();
            }
            else
            {
                logger?.Log("hideOnStart is disabled, NOT hiding the loading screen", this);
            }
        }
        else
        {
            logger?.Log("UIDocument not found in LoadingScreenController", this, Logging.LogType.Error);
        }
    }
    
    private void OnEnable()
    {
        // Suscribirse a los eventos del sistema de loading screen
        LoadingScreenEvents.OnShowLoadingScreen += Show;
        LoadingScreenEvents.OnHideLoadingScreen += Hide;
        LoadingScreenEvents.OnHideLoadingScreenWithDelay += HandleHideWithDelay;
        
        logger?.Log("LoadingScreenController subscribed to events", this);
    }
    
    private void OnDisable()
    {
        // Desuscribirse de los eventos
        LoadingScreenEvents.OnShowLoadingScreen -= Show;
        LoadingScreenEvents.OnHideLoadingScreen -= Hide;
        LoadingScreenEvents.OnHideLoadingScreenWithDelay -= HandleHideWithDelay;
        
        logger?.Log("LoadingScreenController unsubscribed from events", this);
    }
    
    /// <summary>
    /// Manejador para el evento de ocultar con delay.
    /// Si el delay es <= 0, usa el displayDuration configurado.
    /// </summary>
    private void HandleHideWithDelay(float customDelay)
    {
        // Si se provee un delay personalizado válido, usarlo temporalmente
        if (customDelay > 0)
        {
            // Cancelar cualquier coroutine anterior
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }
            
            activeCoroutine = StartCoroutine(HideWithCustomDelayCoroutine(customDelay));
        }
        else
        {
            // Usar el método normal que usa el displayDuration configurado
            HideWithDelay();
        }
    }
    
    /// <summary>
    /// Coroutine para ocultar con un delay personalizado
    /// </summary>
    private IEnumerator HideWithCustomDelayCoroutine(float customDelay)
    {
        logger?.Log($"HideWithCustomDelay started - waiting {customDelay}s", this);
        
        // Esperar la duración especificada
        yield return new WaitForSeconds(customDelay);
        
        logger?.Log("Starting fade out", this);
        
        // Fade out
        yield return StartCoroutine(FadeOut());
        
        activeCoroutine = null;
    }
    
    /// <summary>
    /// Muestra el loading screen inmediatamente.
    /// </summary>
    public void Show()
    {
        logger?.Log($"Show() called - rootElement is null: {rootElement == null}", this);
        
        if (rootElement != null)
        {
            rootElement.style.display = DisplayStyle.Flex;
            rootElement.style.opacity = 1f;
            logger?.Log("Loading screen shown successfully", this);
        }
        else
        {
            logger?.Log("ERROR: rootElement is null, cannot show loading screen", this, Logging.LogType.Error);
            
            // Intentar reinicializar
            if (loadingScreenDocument != null)
            {
                rootElement = loadingScreenDocument.rootVisualElement;
                    logger?.Log($"Retry initialization - rootElement is now null: {rootElement == null}", this);                if (rootElement != null)
                {
                    rootElement.style.display = DisplayStyle.Flex;
                    rootElement.style.opacity = 1f;
                    logger?.Log("Loading screen shown after reinitialization", this);
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
            logger?.Log("Loading screen hidden", this);
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
        logger?.Log($"HideWithDelay started - waiting {displayDuration}s", this);
        
        // Esperar la duración especificada
        yield return new WaitForSeconds(displayDuration);
        
        logger?.Log("Starting fade out", this);
        
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
