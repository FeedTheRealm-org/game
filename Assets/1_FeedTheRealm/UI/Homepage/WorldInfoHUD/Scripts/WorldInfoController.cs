using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    public void SetCurrentWorld(FTRShared.Runtime.Models.WorldMetadata world)
    {
        logger.Log($"Setting current world info: {world.name}", this);

        WorldNameLabel.text = world.name.Split('.')[0];
        WorldDescriptionLabel.text = world.description;
        WorldCreatedAtLabel.text = $"Created {makeHumanReadableCreatedAt(world.createdAt)}";

        _ = getUserDisplayName(world.userId);
    }

    private async Task getUserDisplayName(string userId)
    {
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
        else
        {
            logger.Log(
                $"Failed to parse createdAt date: {createdAt}",
                this,
                Logging.LogType.Warning
            );
            return createdAt;
        }
    }

    private void OnEnable()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;

        SideBar = ui.Q<VisualElement>("SideBar");
        if (SideBar == null)
        {
            logger.Log("SideBar element not found in UI", this, Logging.LogType.Warning);
            return;
        }
        SideBar.RegisterCallback<GeometryChangedEvent>(OnSideBarGeometryChanged);

        CloseButton = ui.Q<Button>("CloseButton");
        if (CloseButton == null)
        {
            logger.Log("CloseButton not found in UI", this, Logging.LogType.Warning);
            return;
        }
        CloseButton.clicked += OnCloseButtonClicked;

        WorldNameLabel = ui.Q<Label>("Title");
        if (WorldNameLabel == null)
        {
            logger.Log("WorldNameLabel not found in UI", this, Logging.LogType.Warning);
        }

        WorldDescriptionLabel = ui.Q<Label>("Description");
        if (WorldDescriptionLabel == null)
        {
            logger.Log("WorldDescriptionLabel not found in UI", this, Logging.LogType.Warning);
        }

        WorldCreatedAtLabel = ui.Q<Label>("CreatedAt");
        if (WorldCreatedAtLabel == null)
        {
            logger.Log("WorldCreatedAtLabel not found in UI", this, Logging.LogType.Warning);
        }

        WorldCreatorLabel = ui.Q<Label>("CreatedBy");
        if (WorldCreatorLabel == null)
        {
            logger.Log("WorldCreatorLabel not found in UI", this, Logging.LogType.Warning);
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

        CloseButton.SetEnabled(false);

        if (sidebarAnimationCoroutine != null)
            StopCoroutine(sidebarAnimationCoroutine);
        sidebarAnimationCoroutine = StartCoroutine(
            AnimateSidebar(
                sidebarWidthPx > 0 ? 0f : 0f,
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
        System.Action onComplete = null
    )
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float x = Mathf.Lerp(fromPx, toPx, eased);
            SideBar.style.left = new StyleLength(new Length(x, LengthUnit.Pixel));
            elapsed += Time.deltaTime;
            yield return null;
        }
        SideBar.style.left = new StyleLength(new Length(toPx, LengthUnit.Pixel));
        onComplete?.Invoke();
        sidebarAnimationCoroutine = null;
    }
}
