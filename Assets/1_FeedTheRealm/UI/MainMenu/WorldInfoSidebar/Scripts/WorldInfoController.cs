using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using API;
using FTRShared.Runtime.Models;
using FTRShared.UI.ZoneStatusBadge;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(ZoneStatusBadgeController))]
public class WorldInfoController : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private API.WorldService worldService;

    [Inject]
    private API.ModelService modelService;

    private ZoneStatusBadgeController zoneStatusBadge;

    private VisualElement ui;
    private VisualElement SideBar;
    private Button CloseButton;
    private Label WorldNameLabel;
    private Label WorldDescriptionLabel;
    private Label WorldCreatedAtLabel;
    private Label WorldCreatorLabel;
    private VisualElement OnlineStatusPill;
    private Label OnlinePlayersLabel;
    private Button JoinButton;
    private Button DownloadButton;
    private Label DownloadingLabel;
    private VisualElement ProgressBarContainer;
    private VisualElement ProgressBarFill;
    private string downloadButtonText;
    private Coroutine sidebarAnimationCoroutine;
    private Coroutine uiInitCoroutine;
    private float sidebarWidthPx = 0f;
    private ActiveWorldData currentWorld;
    private ZoneStatusBadgeController.State pendingStatus = ZoneStatusBadgeController.State.Offline;
    private int? pendingOnlineZones;
    private int? pendingTotalZones;
    private Func<ActiveWorldData, Task> joinHandler;
    private bool isDownloading;

    public void SetCurrentWorld(
        ActiveWorldData activeWorld,
        ZoneStatusBadgeController.State? status = null,
        int? onlineZones = null,
        int? totalZones = null,
        Func<ActiveWorldData, Task> joinHandler = null
    )
    {
        if (activeWorld?.worldData == null)
        {
            logger.Log("SetCurrentWorld called with null world.", this, Logging.LogType.Warning);
            return;
        }

        currentWorld = activeWorld;
        if (status.HasValue)
            pendingStatus = status.Value;
        if (onlineZones.HasValue)
            pendingOnlineZones = onlineZones.Value;
        if (totalZones.HasValue)
            pendingTotalZones = totalZones.Value;
        if (joinHandler != null)
            this.joinHandler = joinHandler;

        if (!TryInitializeUiReferences())
        {
            logger.Log(
                "WorldInfo UI references are not ready yet; world info will be applied when available.",
                this,
                Logging.LogType.Warning
            );
            EnsureUiInitialized();
            return;
        }

        ApplyWorldInfo(activeWorld.worldData);
    }

    private void ApplyWorldInfo(WorldData world)
    {
        logger.Log($"Setting current world info: {world.worldName}", this);

        string worldName = string.IsNullOrWhiteSpace(world.worldName)
            ? "Unknown World"
            : world.worldName;
        WorldNameLabel.text = worldName.Split('.')[0];
        WorldDescriptionLabel.text = string.IsNullOrWhiteSpace(world.description)
            ? "No description provided."
            : world.description;

        string createdAtText = world.created_at == default ? "" : world.created_at.ToString("o");
        WorldCreatedAtLabel.text = string.IsNullOrWhiteSpace(createdAtText)
            ? "unknown date"
            : makeHumanReadableCreatedAt(createdAtText);

        ApplyOnlineStatus();
        ApplyDownloadStatus();

        if (!string.IsNullOrWhiteSpace(world.worldId))
            _ = RefreshActivePlayersAsync(world.worldId);

        if (string.IsNullOrWhiteSpace(world.created_by))
        {
            WorldCreatorLabel.text = "Unknown User";
            return;
        }

        _ = getUserDisplayName(world.created_by);
    }

    private async Task RefreshActivePlayersAsync(string worldId)
    {
        if (worldService == null)
        {
            logger.Log(
                "WorldService is not assigned in WorldInfoController.",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        (int activePlayers, long statusCode) = await worldService.GetActivePlayers(worldId);
        if (statusCode == 200)
        {
            pendingOnlineZones = activePlayers;
            ApplyOnlineStatus();
        }
    }

    private bool TryInitializeUiReferences()
    {
        var document = GetComponent<UIDocument>();
        if (document == null || document.rootVisualElement == null)
        {
            logger.Log("UIDocument root is not available.", this, Logging.LogType.Warning);
            return false;
        }

        ui = document.rootVisualElement;
        if (ui.childCount == 0)
            return false;

        SideBar = ui.Q<VisualElement>("SideBar");
        if (SideBar == null)
        {
            logger.Log("SideBar element not found in UI", this, Logging.LogType.Warning);
            return false;
        }

        SideBar.UnregisterCallback<GeometryChangedEvent>(OnSideBarGeometryChanged);
        SideBar.RegisterCallback<GeometryChangedEvent>(OnSideBarGeometryChanged);

        CloseButton = ui.Q<Button>("CloseButton");
        if (CloseButton == null)
        {
            logger.Log("CloseButton not found in UI", this, Logging.LogType.Warning);
            return false;
        }

        CloseButton.clicked -= OnCloseButtonClicked;
        CloseButton.clicked += OnCloseButtonClicked;

        WorldNameLabel = ui.Q<Label>("Title");
        WorldDescriptionLabel = ui.Q<Label>("Description");
        WorldCreatedAtLabel = ui.Q<Label>("CreatedAt");
        WorldCreatorLabel = ui.Q<Label>("CreatedBy");
        OnlineStatusPill = ui.Q<VisualElement>("OnlineStatusPill");
        OnlinePlayersLabel = ui.Q<Label>("OnlinePlayers");
        JoinButton = ui.Q<Button>("JoinButton");
        DownloadButton = ui.Q<Button>("DownloadButton");
        DownloadingLabel = ui.Q<Label>("DownloadingLabel");
        ProgressBarContainer = ui.Q<VisualElement>("ProgressBarContainer");
        ProgressBarFill = ui.Q<VisualElement>("ProgressBarFill");

        if (WorldNameLabel == null)
        {
            logger.Log("WorldName label not found in UI.", this, Logging.LogType.Warning);
            return false;
        }

        if (WorldDescriptionLabel == null)
        {
            logger.Log("WorldDescription label not found in UI.", this, Logging.LogType.Warning);
            return false;
        }

        if (WorldCreatedAtLabel == null)
        {
            logger.Log("WorldCreatedAt label not found in UI.", this, Logging.LogType.Warning);
            return false;
        }

        if (WorldCreatorLabel == null)
        {
            logger.Log("WorldCreator label not found in UI.", this, Logging.LogType.Warning);
            return false;
        }

        if (OnlineStatusPill == null)
            logger.Log("OnlineStatusPill not found in UI.", this, Logging.LogType.Warning);
        if (OnlinePlayersLabel == null)
            logger.Log("OnlinePlayers label not found in UI.", this, Logging.LogType.Warning);
        if (JoinButton == null)
            logger.Log("JoinButton not found in UI.", this, Logging.LogType.Warning);
        if (DownloadButton == null)
            logger.Log("DownloadButton not found in UI.", this, Logging.LogType.Warning);
        if (DownloadingLabel == null)
            logger.Log("DownloadingLabel not found in UI.", this, Logging.LogType.Warning);
        if (ProgressBarContainer == null)
            logger.Log("ProgressBarContainer not found in UI.", this, Logging.LogType.Warning);
        if (ProgressBarFill == null)
            logger.Log("ProgressBarFill not found in UI.", this, Logging.LogType.Warning);

        if (JoinButton != null)
        {
            JoinButton.clicked -= OnJoinButtonClicked;
            JoinButton.clicked += OnJoinButtonClicked;
        }

        if (DownloadButton != null)
        {
            if (string.IsNullOrEmpty(downloadButtonText))
                downloadButtonText = DownloadButton.text;
            DownloadButton.clicked -= OnDownloadButtonClicked;
            DownloadButton.clicked += OnDownloadButtonClicked;
        }

        if (zoneStatusBadge == null)
            zoneStatusBadge = GetComponent<ZoneStatusBadgeController>();

        return true;
    }

    private async Task getUserDisplayName(string userId)
    {
        if (WorldCreatorLabel == null)
            return;

        if (playerService == null)
        {
            logger.Log(
                "PlayerService is not assigned in WorldInfoController.",
                this,
                Logging.LogType.Warning
            );
            WorldCreatorLabel.text = "Unknown User";
            return;
        }

        API.CharacterInfoResponse characterInfo = await playerService.GetCharacterInfoAsync(userId);

        if (characterInfo == null)
        {
            logger.Log(
                $"Failed to fetch character info for userId '{userId}'; using fallback display name.",
                this,
                Logging.LogType.Warning
            );
            WorldCreatorLabel.text = "Unknown User";
            return;
        }

        string displayName = characterInfo.character_name;
        if (string.IsNullOrEmpty(displayName))
        {
            logger.Log(
                $"Character info for userId '{userId}' has no character_name; using fallback display name.",
                this,
                Logging.LogType.Warning
            );
            WorldCreatorLabel.text = "Unknown User";
            return;
        }

        WorldCreatorLabel.text = displayName;
    }

    private void ApplyOnlineStatus()
    {
        if (OnlineStatusPill == null || OnlinePlayersLabel == null)
            return;

        if (zoneStatusBadge != null)
        {
            OnlineStatusPill.Clear();
            OnlineStatusPill.Add(zoneStatusBadge.Create(pendingStatus));
        }

        int online = pendingOnlineZones ?? 0;
        OnlinePlayersLabel.text = online.ToString();
    }

    private void ApplyDownloadStatus()
    {
        if (DownloadingLabel != null)
        {
            DownloadingLabel.text = "";
            DownloadingLabel.style.display = DisplayStyle.None;
        }

        if (ProgressBarContainer != null)
            ProgressBarContainer.style.display = DisplayStyle.None;

        if (ProgressBarFill != null)
            ProgressBarFill.style.width = new StyleLength(new Length(0, LengthUnit.Percent));

        if (DownloadButton != null)
        {
            DownloadButton.text = string.IsNullOrEmpty(downloadButtonText)
                ? DownloadButton.text
                : downloadButtonText;
            DownloadButton.SetEnabled(true);
        }
    }

    private void OnJoinButtonClicked()
    {
        if (currentWorld?.worldData == null)
            return;

        _ = joinHandler?.Invoke(currentWorld);
    }

    private async void OnDownloadButtonClicked()
    {
        if (isDownloading)
            return;

        if (currentWorld?.worldData == null)
            return;

        if (modelService == null)
        {
            logger.Log(
                "ModelService is not assigned in WorldInfoController.",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        string worldId = currentWorld.worldData.worldId;
        if (string.IsNullOrWhiteSpace(worldId))
            return;

        isDownloading = true;
        try
        {
            if (DownloadButton != null)
            {
                DownloadButton.text = "...";
                DownloadButton.SetEnabled(false);
            }

            if (DownloadingLabel != null)
            {
                DownloadingLabel.text = "downloading content...";
                DownloadingLabel.style.display = DisplayStyle.Flex;
            }

            if (ProgressBarContainer != null)
                ProgressBarContainer.style.display = DisplayStyle.Flex;

            if (ProgressBarFill != null)
                ProgressBarFill.style.width = new StyleLength(new Length(0, LengthUnit.Percent));

            Dictionary<string, ModelInfo> models = await modelService.ListWorldModels(worldId);
            int total = models?.Count ?? 0;
            if (total == 0)
            {
                if (ProgressBarFill != null)
                    ProgressBarFill.style.width = new StyleLength(
                        new Length(100, LengthUnit.Percent)
                    );
                return;
            }

            int completed = 0;
            foreach (var model in models.Values)
            {
                string modelName = string.IsNullOrWhiteSpace(model.url)
                    ? model.model_id
                    : Path.GetFileName(model.url);

                await modelService.DownloadModel(model);
                completed++;
                if (DownloadingLabel != null)
                    DownloadingLabel.text =
                        $"downloading content... {completed}/{total} {modelName}";
                if (ProgressBarFill != null)
                {
                    float percent = Mathf.Clamp01((float)completed / total) * 100f;
                    ProgressBarFill.style.width = new StyleLength(
                        new Length(percent, LengthUnit.Percent)
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Model download failed: {ex.Message}", this, Logging.LogType.Warning);
        }
        finally
        {
            if (DownloadingLabel != null)
                DownloadingLabel.style.display = DisplayStyle.None;

            if (ProgressBarContainer != null)
                ProgressBarContainer.style.display = DisplayStyle.None;

            if (DownloadButton != null)
            {
                DownloadButton.text = string.IsNullOrEmpty(downloadButtonText)
                    ? DownloadButton.text
                    : downloadButtonText;
                DownloadButton.SetEnabled(true);
            }

            isDownloading = false;
        }
    }

    private string makeHumanReadableCreatedAt(string createdAt)
    {
        if (DateTime.TryParse(createdAt, out DateTime parsedDate))
        {
            TimeSpan span = DateTime.UtcNow - parsedDate.ToUniversalTime();

            if (span.TotalSeconds < 60)
                return "just now";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} minute{((int)span.TotalMinutes == 1 ? "" : "s")} ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} hour{((int)span.TotalHours == 1 ? "" : "s")} ago";
            if (span.TotalDays < 30)
                return $"{(int)span.TotalDays} day{((int)span.TotalDays == 1 ? "" : "s")} ago";

            if (span.TotalDays < 365)
            {
                int months = (int)(span.TotalDays / 30);
                return $"{months} month{(months == 1 ? "" : "s")} ago";
            }

            int years = (int)(span.TotalDays / 365);
            return $"{years} year{(years == 1 ? "" : "s")} ago";
        }

        logger.Log($"Failed to parse createdAt date: {createdAt}", this, Logging.LogType.Warning);
        return createdAt;
    }

    private void OnEnable()
    {
        EnsureUiInitialized();
    }

    private void OnDisable()
    {
        if (CloseButton != null)
        {
            CloseButton.clicked -= OnCloseButtonClicked;
        }

        if (JoinButton != null)
            JoinButton.clicked -= OnJoinButtonClicked;

        if (DownloadButton != null)
            DownloadButton.clicked -= OnDownloadButtonClicked;

        if (SideBar != null)
        {
            SideBar.UnregisterCallback<GeometryChangedEvent>(OnSideBarGeometryChanged);
        }

        if (sidebarAnimationCoroutine != null)
        {
            StopCoroutine(sidebarAnimationCoroutine);
            sidebarAnimationCoroutine = null;
        }

        if (uiInitCoroutine != null)
        {
            StopCoroutine(uiInitCoroutine);
            uiInitCoroutine = null;
        }
    }

    private void EnsureUiInitialized()
    {
        if (uiInitCoroutine != null)
            return;

        uiInitCoroutine = StartCoroutine(InitializeUiWhenReady());
    }

    private IEnumerator InitializeUiWhenReady()
    {
        while (isActiveAndEnabled && !TryInitializeUiReferences())
            yield return null;

        if (currentWorld != null && TryInitializeUiReferences())
        {
            ApplyWorldInfo(currentWorld.worldData);
        }

        uiInitCoroutine = null;
    }

    private void OnCloseButtonClicked()
    {
        logger.Log("Close button clicked", this);
        if (SideBar == null)
        {
            gameObject.SetActive(false);
            return;
        }

        CloseButton?.SetEnabled(false);

        if (sidebarAnimationCoroutine != null)
            StopCoroutine(sidebarAnimationCoroutine);
        sidebarAnimationCoroutine = StartCoroutine(
            AnimateSidebar(
                0f,
                -sidebarWidthPx,
                0.28f,
                () =>
                {
                    gameObject.SetActive(false);
                }
            )
        );
    }

    private void OnSideBarGeometryChanged(GeometryChangedEvent evt)
    {
        SideBar.UnregisterCallback<GeometryChangedEvent>(OnSideBarGeometryChanged);

        sidebarWidthPx = SideBar.resolvedStyle.width;
        SideBar.style.position = Position.Absolute;
        SideBar.style.top = new StyleLength(new Length(0, LengthUnit.Pixel));
        SideBar.style.height = new StyleLength(new Length(evt.newRect.height, LengthUnit.Pixel));
        SideBar.style.width = new StyleLength(new Length(sidebarWidthPx, LengthUnit.Pixel));

        SideBar.style.left = new StyleLength(new Length(-sidebarWidthPx, LengthUnit.Pixel));
        if (sidebarAnimationCoroutine != null)
            StopCoroutine(sidebarAnimationCoroutine);
        sidebarAnimationCoroutine = StartCoroutine(
            AnimateSidebar(
                -sidebarWidthPx,
                0f,
                0.28f,
                () =>
                {
                    CloseButton?.SetEnabled(true);
                }
            )
        );
    }

    private IEnumerator AnimateSidebar(
        float fromPx,
        float toPx,
        float duration,
        Action onComplete = null
    )
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (SideBar == null)
            {
                sidebarAnimationCoroutine = null;
                yield break;
            }

            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float x = Mathf.Lerp(fromPx, toPx, eased);
            SideBar.style.left = new StyleLength(new Length(x, LengthUnit.Pixel));
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (SideBar == null)
        {
            sidebarAnimationCoroutine = null;
            yield break;
        }

        SideBar.style.left = new StyleLength(new Length(toPx, LengthUnit.Pixel));
        onComplete?.Invoke();
        sidebarAnimationCoroutine = null;
    }
}
