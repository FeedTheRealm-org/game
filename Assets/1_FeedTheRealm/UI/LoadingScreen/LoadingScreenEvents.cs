using System;
using UnityEngine;

/// <summary>
/// Sistema de eventos para controlar el Loading Screen de forma desacoplada.
/// Permite que cualquier sistema dispare el mostrar/ocultar del loading screen.
/// </summary>
public static class LoadingScreenEvents
{
    /// <summary>
    /// Evento que se dispara cuando se debe mostrar el loading screen
    /// </summary>
    public static event Action OnShowLoadingScreen;
    
    /// <summary>
    /// Evento que se dispara cuando se debe ocultar el loading screen inmediatamente
    /// </summary>
    public static event Action OnHideLoadingScreen;
    
    /// <summary>
    /// Evento que se dispara cuando se debe ocultar el loading screen con delay/fade
    /// Incluye un parámetro opcional de delay en segundos
    /// </summary>
    public static event Action<float> OnHideLoadingScreenWithDelay;
    
    /// <summary>
    /// Dispara el evento para mostrar el loading screen
    /// </summary>
    public static void Show()
    {
        Debug.Log("[LoadingScreenEvents] Show event triggered");
        OnShowLoadingScreen?.Invoke();
    }
    
    /// <summary>
    /// Dispara el evento para ocultar el loading screen inmediatamente
    /// </summary>
    public static void Hide()
    {
        Debug.Log("[LoadingScreenEvents] Hide event triggered");
        OnHideLoadingScreen?.Invoke();
    }
    
    /// <summary>
    /// Dispara el evento para ocultar el loading screen con delay
    /// </summary>
    /// <param name="delay">Delay en segundos antes de empezar el fade out. Si es 0 o negativo, usa el delay por defecto del controller</param>
    public static void HideWithDelay(float delay = -1f)
    {
        Debug.Log($"[LoadingScreenEvents] HideWithDelay event triggered (delay: {delay}s)");
        OnHideLoadingScreenWithDelay?.Invoke(delay);
    }
    
    /// <summary>
    /// Limpia todos los suscriptores (útil para testing o cuando se cambia de escena)
    /// </summary>
    public static void ClearAllSubscriptions()
    {
        OnShowLoadingScreen = null;
        OnHideLoadingScreen = null;
        OnHideLoadingScreenWithDelay = null;
        Debug.Log("[LoadingScreenEvents] All subscriptions cleared");
    }
}
