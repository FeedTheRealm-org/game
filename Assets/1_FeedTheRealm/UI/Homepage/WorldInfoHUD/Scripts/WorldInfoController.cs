using System;
using System.Collections;
using System.Threading.Tasks;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WorldInfoController : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private API.PlayerService playerService;

    private VisualElement ui;
    private VisualElement SideBar;
    private Button CloseButton;
    private Label WorldNameLabel;
    private Label WorldDescriptionLabel;
    private Label WorldCreatedAtLabel;
    private Label WorldCreatorLabel;
    private Coroutine sidebarAnimationCoroutine;
    private float sidebarWidthPx = 0f;
    private WorldData pendingWorld;

    public void SetCurrentWorld(WorldData world)
    {
        if (world == null)
        {
            logger.Log("SetCurrentWorld called with null world.", this, Logging.LogType.Warning);
            return;
        }

        pendingWorld = world;

        if (!TryInitializeUiReferences())
        {
            logger.Log(
                "WorldInfo UI references are not ready yet; world info will be applied when available.",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        ApplyWorldInfo(world);
        pendingWorld = null;
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
            ? "Created unknown date"
            : $"Created {makeHumanReadableCreatedAt(createdAtText)}";

        if (string.IsNullOrWhiteSpace(world.created_by))
        {
            WorldCreatorLabel.text = "Created By Unknown User";
            return;
        }

        _ = getUserDisplayName(world.created_by);
    }

    private bool TryInitializeUiReferences()
    {
        if (ui == null)
        {
            var document = GetComponent<UIDocument>();
            if (document == null || document.rootVisualElement == null)
            {
                logger.Log("UIDocument root is not available.", this, Logging.LogType.Warning);
                return false;
            }

            ui = document.rootVisualElement;
        }

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

        if (
            WorldNameLabel == null
            || WorldDescriptionLabel == null
            || WorldCreatedAtLabel == null
            || WorldCreatorLabel == null
        )
        {
            logger.Log(
                "One or more world info labels are missing in the UI.",
                this,
                Logging.LogType.Warning
            );
            return false;
        }

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
            WorldCreatorLabel.text = "Created By Unknown User";
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
            WorldCreatorLabel.text = "Created By Unknown User";
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
            WorldCreatorLabel.text = "Created By Unknown User";
            return;
        }

        WorldCreatorLabel.text = $"Created By {displayName}";
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
        if (!TryInitializeUiReferences())
        {
            return;
        }

        if (pendingWorld != null)
        {
            ApplyWorldInfo(pendingWorld);
            pendingWorld = null;
        }
    }

    private void OnDisable()
    {
        if (CloseButton != null)
        {
            CloseButton.clicked -= OnCloseButtonClicked;
        }

        if (SideBar != null)
        {
            SideBar.UnregisterCallback<GeometryChangedEvent>(OnSideBarGeometryChanged);
        }

        if (sidebarAnimationCoroutine != null)
        {
            StopCoroutine(sidebarAnimationCoroutine);
            sidebarAnimationCoroutine = null;
        }
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
