using System.Collections;
using FeedTheRealm.Core.EventChannels.Setup;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
/// <summary>
///  This class is responsible for controlling the loading screen UI. It listens to the LoadingEvent and shows/hides the loading screen accordingly.
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    [SerializeField]
    private LoadingEvent loadingEvent;

    private VisualElement rootElement;

    private void OnEnable()
    {
        rootElement = GetComponent<UIDocument>().rootVisualElement;
        ToggleLoadingScreen(false);
        loadingEvent.OnRaised += ToggleLoadingScreen;
        Debug.Log($"[LoadingScreenController] OnEnable called. Subscribing to loading event.");
    }

    private void OnDisable()
    {
        loadingEvent.OnRaised -= ToggleLoadingScreen;
    }

    private void ToggleLoadingScreen(bool show)
    {
        Debug.Log($"[LoadingScreenController] Toggle Loading Screen: {show}");
        if (show)
            rootElement.style.display = DisplayStyle.Flex;
        else
            rootElement.style.display = DisplayStyle.None;
    }
}
