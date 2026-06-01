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

    [SerializeField]
    private LoadingProgressEvent loadingProgressEvent;

    private VisualElement rootElement;
    private ProgressBar progressBar;

    private void Awake()
    {
        rootElement = GetComponent<UIDocument>().rootVisualElement;
        progressBar = rootElement.Q<ProgressBar>("loading-progress");
        loadingEvent.OnRaised += ToggleLoadingScreen;
        if (loadingProgressEvent != null)
            loadingProgressEvent.OnRaised += UpdateProgress;
        Debug.Log($"[LoadingScreenController] Awake called. Subscribing to loading event.");
    }

    private void OnDisable()
    {
        loadingEvent.OnRaised -= ToggleLoadingScreen;
        if (loadingProgressEvent != null)
            loadingProgressEvent.OnRaised -= UpdateProgress;
    }

    private void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
            progressBar.title = $"Loading... {(int)(progress * 100)}%";
        }
    }

    private void ToggleLoadingScreen(bool show)
    {
        Debug.Log($"[LoadingScreenController] Toggle Loading Screen: {show}");
        if (show)
        {
            if (progressBar != null)
            {
                progressBar.value = 0f;
                progressBar.title = "Loading... 0%";
            }
            rootElement.style.display = DisplayStyle.Flex;
        }
        else
        {
            rootElement.style.display = DisplayStyle.None;
            Destroy(gameObject);
        }
    }
}
