using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Common.Config;
using FTR.Gameplay.Client.EntryPoints;
using FTR.Gameplay.Common.Characters.Shared.Portal;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WorldFeedMenuController : MonoBehaviour, IMainMenuController
{
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private Config config;

    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private API.WorldService worldService;

    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private SceneReference worldScene;

    [SerializeField]
    private WorldSelector worldSelector;

    [SerializeField]
    private TeleportDataPersistence teleportDataPersistence;

    [SerializeField]
    private ItemAssetsService itemAssetsService;

    [SerializeField]
    private GameObject worldInfoHUD;

    public event Action OnNavigateToWorld;

    private VisualElement ui;
    private TextField searchField;
    private Button backButton;
    private Button forwardButton;
    private ScrollView listOfWorlds;

    private int currentOffset = 0;
    private int maxPageOffset = int.MaxValue;
    private const int PAGE_SIZE = 20;

    private enum WorldBadgeState
    {
        Online,
        Degraded,
        Offline,
    }

    private void Awake()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
        teleportDataPersistence.PortalId = null;

        searchField = ui.Q<TextField>("SearchField");
        backButton = ui.Q<Button>("BackButton");
        forwardButton = ui.Q<Button>("ForwardButton");
        listOfWorlds = ui.Q<ScrollView>("ListOfWorlds");

        searchField?.RegisterValueChangedCallback(evt =>
        {
            currentOffset = 0;
            maxPageOffset = int.MaxValue;
            _ = RenderWorldPage(currentOffset, evt.newValue);
        });

        if (backButton != null)
            backButton.clicked += OnBackButtonClicked;
        if (forwardButton != null)
            forwardButton.clicked += OnForwardButtonClicked;
    }

    private async void OnEnable()
    {
        logger.Log("[WorldFeed] World feed menu opened.", this);
        worldSelector?.ClearSelectedWorldJoinToken();
        await RenderWorldPage(currentOffset, searchField?.value);
        logger.Log("[WorldFeed] World feed menu rendered.", this);
    }

    private async Task RenderWorldPage(int offset, string filter = null)
    {
        var (activeWorlds, error) = await worldService.GetActiveWorlds(
            offset,
            PAGE_SIZE,
            filter,
            session.APIToken
        );

        logger.Log(
            $"[WorldFeed] Fetched worlds with offset {offset}, filter '{filter}'. Received {activeWorlds?.Count ?? 0} worlds. Error: {error}"
        );

        if (!string.IsNullOrEmpty(error))
        {
            logger.Log($"[WorldFeed] Error fetching worlds: {error}", this, Logging.LogType.Error);
            return;
        }

        if (activeWorlds == null || activeWorlds.Count == 0)
        {
            if (offset > 0)
            {
                maxPageOffset = offset - PAGE_SIZE;
                currentOffset = maxPageOffset;
                await RenderWorldPage(currentOffset, filter);
            }
            else
            {
                maxPageOffset = 0;
                RenderWorlds(new List<ActiveWorldData>(), null);
            }
            return;
        }

        currentOffset = offset;

        if (activeWorlds.Count < PAGE_SIZE)
            maxPageOffset = offset;

        Dictionary<string, WorldBadgeState> worldBadgeStates =
            new Dictionary<string, WorldBadgeState>();
        try
        {
            var (_, worldsWithZones, pageError) = await worldService.GetWorldPage(
                0,
                PAGE_SIZE,
                filter,
                session.APIToken
            );

            if (string.IsNullOrEmpty(pageError) && worldsWithZones != null)
            {
                foreach (var w in worldsWithZones)
                    worldBadgeStates[w.id] = GetWorldBadgeState(w.zones);
            }
        }
        catch (Exception ex)
        {
            logger.Log(
                $"[WorldFeed] Could not fetch zone metadata for badges: {ex.Message}",
                this,
                Logging.LogType.Warning
            );
        }

        RenderWorlds(activeWorlds, worldBadgeStates);
    }

    private void RenderWorlds(
        List<ActiveWorldData> activeWorlds,
        Dictionary<string, WorldBadgeState> worldBadgeStates
    )
    {
        if (listOfWorlds == null)
        {
            logger.Log(
                "[WorldFeed] ListOfWorlds UI element not found.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        listOfWorlds.Clear();

        if (activeWorlds.Count == 0)
        {
            var noResults = new Label("No active worlds found");
            noResults.AddToClassList("noResultsMessage");
            listOfWorlds.Add(noResults);
            SetPaginationVisible(false);
            return;
        }

        foreach (var activeWorld in activeWorlds)
        {
            WorldBadgeState state = WorldBadgeState.Offline;
            if (worldBadgeStates != null && activeWorld.worldData?.worldId != null)
                worldBadgeStates.TryGetValue(activeWorld.worldData.worldId, out state);

            var element = CreateWorldElement(activeWorld, state);
            if (element != null)
                listOfWorlds.Add(element);
        }

        bool showPagination = activeWorlds.Count >= PAGE_SIZE || currentOffset > 0;
        SetPaginationVisible(showPagination);
    }

    private VisualElement CreateWorldElement(
        ActiveWorldData activeWorld,
        WorldBadgeState badgeState
    )
    {
        var worldData = activeWorld.worldData;
        if (worldData == null || string.IsNullOrWhiteSpace(worldData.worldName))
            return null;

        var element = new VisualElement();
        element.AddToClassList("worldElement");
        element.name = "WorldElement";

        var label = new Label(worldData.worldName.Split('.')[0]);
        label.AddToClassList("worldName");
        label.name = "WorldName";

        var statusBadge = new Label();
        statusBadge.name = "StatusBadge";
        SetStatusBadge(statusBadge, badgeState);

        var aboutButton = new Button();
        aboutButton.AddToClassList("aboutButton");
        aboutButton.name = "AboutButton";
        aboutButton.text = "i";
        aboutButton.clicked += () => OnClickAboutWorld(worldData);
        aboutButton.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

        element.Add(label);
        element.Add(statusBadge);
        element.Add(aboutButton);
        element.AddManipulator(new Clickable(() => _ = OnWorldSelected(activeWorld)));

        return element;
    }

    private async Task OnWorldSelected(ActiveWorldData activeWorld)
    {
        var worldData = activeWorld.worldData;

        if (worldData == null || string.IsNullOrWhiteSpace(worldData.worldId))
        {
            logger.Log("[WorldFeed] Selected world is invalid.", this, Logging.LogType.Warning);
            return;
        }

        logger.Log(
            $"[WorldFeed] Selected world: {worldData.worldName}",
            this,
            Logging.LogType.Info
        );

        try
        {
            worldSelector.SetSelectedWorldId(worldData.worldId);
            worldSelector.SetSelectedZoneId(worldData.startingZone);
            config.CurrentServerAddress = activeWorld.zoneAddress.ip;
            config.CurrentServerPort = (ushort)activeWorld.zoneAddress.port;
            SetWorldIdForServices(worldData.worldId);

            var worldJoinToken = await playerService.IssueWorldJoinTokenAsync(worldData.worldId);
            if (worldJoinToken == null || string.IsNullOrWhiteSpace(worldJoinToken.token_id))
            {
                logger.Log(
                    "[WorldFeed] Failed to issue world join token.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            worldSelector.SetSelectedWorldJoinToken(worldJoinToken.token_id);

            // activeWorld.zoneAddress is ready here for your zone addressing logic
            logger.Log(
                $"[WorldFeed] Zone address: {activeWorld.zoneAddress.ip}:{activeWorld.zoneAddress.port}",
                this,
                Logging.LogType.Info
            );

            if (OnNavigateToWorld != null)
                OnNavigateToWorld.Invoke();
            else
                SceneManager.LoadScene(worldScene.SceneName);
        }
        catch (Exception ex)
        {
            logger.Log(
                $"[WorldFeed] Exception selecting world: {ex.Message}",
                this,
                Logging.LogType.Error
            );
        }
    }

    private void SetWorldIdForServices(string worldId)
    {
        itemAssetsService?.SetCurrentWorldId(worldId);
    }

    private void OnClickAboutWorld(WorldData world)
    {
        if (world == null)
            return;

        logger.Log($"[WorldFeed] About clicked for world: {world.worldName}", this);

        if (worldInfoHUD == null)
        {
            logger.Log(
                "[WorldFeed] WorldInfoHUD reference is not assigned.",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        var worldInfoController = worldInfoHUD.GetComponent<WorldInfoController>();
        if (worldInfoController == null)
        {
            logger.Log(
                "[WorldFeed] WorldInfoController component not found on WorldInfoHUD.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        worldInfoHUD.SetActive(true);
        worldInfoController.SetCurrentWorld(world);
    }

    private void OnBackButtonClicked()
    {
        if (currentOffset < PAGE_SIZE)
        {
            logger.Log("[WorldFeed] Already on first page.", this, Logging.LogType.Warning);
            return;
        }

        currentOffset -= PAGE_SIZE;
        _ = RenderWorldPage(currentOffset, searchField?.value);
    }

    private void OnForwardButtonClicked()
    {
        if (currentOffset >= maxPageOffset)
        {
            logger.Log("[WorldFeed] Already on last page.", this, Logging.LogType.Warning);
            return;
        }

        currentOffset += PAGE_SIZE;
        _ = RenderWorldPage(currentOffset, searchField?.value);
    }

    private void SetPaginationVisible(bool visible)
    {
        var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (backButton != null)
            backButton.style.display = display;
        if (forwardButton != null)
            forwardButton.style.display = display;
    }

    // ── Badge helpers ─────────────────────────────────────────────────────────

    private static WorldBadgeState GetWorldBadgeState(List<WorldZoneMetadata> zones)
    {
        if (zones == null || zones.Count == 0)
            return WorldBadgeState.Offline;

        int onlineCount = 0;
        foreach (var z in zones)
            if (z.is_online)
                onlineCount++;

        if (onlineCount == 0)
            return WorldBadgeState.Offline;
        if (onlineCount == zones.Count)
            return WorldBadgeState.Online;
        return WorldBadgeState.Degraded;
    }

    private static void SetStatusBadge(Label badge, WorldBadgeState state)
    {
        // Stop any existing blink
        if (badge.userData is IVisualElementScheduledItem existing)
        {
            existing.Pause();
            badge.userData = null;
        }

        var green = new Color(0.20f, 0.85f, 0.40f, 1f);
        var yellow = new Color(1.00f, 0.80f, 0.10f, 1f);
        var red = new Color(0.90f, 0.25f, 0.25f, 1f);
        var greenBg = new Color(0.10f, 0.35f, 0.15f, 0.55f);
        var yellowBg = new Color(0.35f, 0.28f, 0.02f, 0.55f);
        var redBg = new Color(0.40f, 0.08f, 0.08f, 0.55f);

        Color dotColor,
            bgColor;
        string labelText;

        switch (state)
        {
            case WorldBadgeState.Online:
                dotColor = green;
                bgColor = greenBg;
                labelText = "Online";
                break;
            case WorldBadgeState.Degraded:
                dotColor = yellow;
                bgColor = yellowBg;
                labelText = "Degraded";
                break;
            default:
                dotColor = red;
                bgColor = redBg;
                labelText = "Offline";
                break;
        }

        // Container
        badge.text = string.Empty;
        badge.style.flexDirection = FlexDirection.Row;
        badge.style.alignItems = Align.Center;
        badge.style.backgroundColor = new StyleColor(bgColor);
        badge.style.borderTopLeftRadius = new StyleLength(10);
        badge.style.borderTopRightRadius = new StyleLength(10);
        badge.style.borderBottomLeftRadius = new StyleLength(10);
        badge.style.borderBottomRightRadius = new StyleLength(10);
        badge.style.paddingLeft = 6;
        badge.style.paddingRight = 6;
        badge.style.paddingTop = 0;
        badge.style.paddingBottom = 0;
        badge.style.marginTop = 0;
        badge.style.marginBottom = 0;
        badge.style.height = 18;
        badge.style.display = DisplayStyle.Flex;
        badge.style.opacity = 1f;

        // Reuse or create children
        Label dot = badge.Q<Label>("BadgeDot");
        Label text = badge.Q<Label>("BadgeText");

        if (dot == null)
        {
            dot = new Label { name = "BadgeDot" };
            dot.style.marginRight = 4;
            badge.Add(dot);
        }

        if (text == null)
        {
            text = new Label { name = "BadgeText" };
            badge.Add(text);
        }

        // Dot
        dot.text = "●";
        dot.style.fontSize = 10;
        dot.style.color = new StyleColor(dotColor);
        dot.style.unityFontStyleAndWeight = FontStyle.Bold;
        dot.style.paddingTop = 0;
        dot.style.paddingBottom = 0;
        dot.style.marginTop = 0;
        dot.style.marginBottom = 0;

        // Text
        text.text = labelText;
        text.style.fontSize = 10;
        text.style.color = new StyleColor(dotColor);
        text.style.unityFontStyleAndWeight = FontStyle.Bold;
        text.style.paddingTop = 0;
        text.style.paddingBottom = 0;
        text.style.marginTop = 0;
        text.style.marginBottom = 0;

        // Blink the dot, always
        bool visible = true;
        var handle = dot
            .schedule.Execute(() =>
            {
                visible = !visible;
                dot.style.opacity = visible ? 1f : 0f;
            })
            .Every(600);

        badge.userData = handle;
    }
}
