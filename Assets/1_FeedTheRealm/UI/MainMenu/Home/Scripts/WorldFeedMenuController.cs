using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Common.Config;
using FTR.Gameplay.Client.EntryPoints;
using FTR.Gameplay.Common.Characters.Shared.Portal;
using FTR.UI;
using FTRShared.Runtime.Models;
using FTRShared.UI.ZoneStatusBadge;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VContainer;

[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(ZoneStatusBadgeController))]
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
    private GameObject confirmPopupPrefab;

    [SerializeField]
    private TeleportDataPersistence teleportDataPersistence;

    [SerializeField]
    private ItemAssetsService itemAssetsService;

    [Inject]
    private WorldInfoMenuHandle worldInfoMenuHandle;
    public event Action OnNavigateToWorld;
    private VisualElement ui;
    private TextField searchField;
    private Button refreshButton;
    private Button backButton;
    private Button forwardButton;
    private ScrollView listOfWorlds;
    private VisualElement worldGrid;
    private ZoneStatusBadgeController zoneStatusBadge;

    private int currentOffset = 0;
    private int maxPageOffset = int.MaxValue;
    private const int PAGE_SIZE = 20;

    private bool hasRenderedOnce;
    private bool isRefreshing;
    private string currentFilter;
    private Vector2 savedScrollOffset;
    private List<ActiveWorldData> cachedWorlds = new();
    private Dictionary<string, ZoneStatusBadgeController.State> cachedBadgeStates = new();
    private Dictionary<string, ZoneCount> cachedZoneCounts = new();

    private struct ZoneCount
    {
        public int online;
        public int total;
    }

    private void Awake()
    {
        zoneStatusBadge = GetComponent<ZoneStatusBadgeController>();
        teleportDataPersistence.PortalId = null;
    }

    private async void OnEnable()
    {
        BindUiElements();
        RegisterUiHandlers();

        if (searchField != null)
            searchField.SetValueWithoutNotify(currentFilter ?? string.Empty);

        logger.Log("[WorldFeed] World feed menu opened.", this);
        worldSelector?.ClearSelectedWorldJoinToken();

        if (hasRenderedOnce)
        {
            RenderWorlds(cachedWorlds, cachedBadgeStates);
            if (listOfWorlds != null)
            {
                listOfWorlds.schedule.Execute(() => listOfWorlds.scrollOffset = savedScrollOffset);
            }
            return;
        }

        currentFilter = searchField?.value;
        await RenderWorldPage(currentOffset, currentFilter);
        logger.Log("[WorldFeed] World feed menu rendered.", this);
    }

    private void OnDisable()
    {
        if (listOfWorlds != null)
            savedScrollOffset = listOfWorlds.scrollOffset;

        UnregisterUiHandlers();
    }

    private void BindUiElements()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
        searchField = ui.Q<TextField>("SearchField");
        refreshButton = ui.Q<Button>("RefreshButton");
        backButton = ui.Q<Button>("BackButton");
        forwardButton = ui.Q<Button>("ForwardButton");
        listOfWorlds = ui.Q<ScrollView>("ListOfWorlds");
        EnsureRuntimeListContainer();
    }

    private void RegisterUiHandlers()
    {
        searchField?.RegisterValueChangedCallback(OnSearchValueChanged);

        if (refreshButton != null)
            refreshButton.clicked += OnRefreshButtonClicked;
        if (backButton != null)
            backButton.clicked += OnBackButtonClicked;
        if (forwardButton != null)
            forwardButton.clicked += OnForwardButtonClicked;
    }

    private void UnregisterUiHandlers()
    {
        searchField?.UnregisterValueChangedCallback(OnSearchValueChanged);
        if (refreshButton != null)
            refreshButton.clicked -= OnRefreshButtonClicked;
        if (backButton != null)
            backButton.clicked -= OnBackButtonClicked;
        if (forwardButton != null)
            forwardButton.clicked -= OnForwardButtonClicked;
    }

    private void OnSearchValueChanged(ChangeEvent<string> evt)
    {
        currentOffset = 0;
        maxPageOffset = int.MaxValue;
        currentFilter = evt.newValue;
        _ = RenderWorldPage(currentOffset, currentFilter);
    }

    private void EnsureRuntimeListContainer()
    {
        if (listOfWorlds == null)
            return;

        listOfWorlds.Clear();
        worldGrid = new VisualElement { name = "WorldGrid" };
        worldGrid.AddToClassList("categoryList");
        listOfWorlds.Add(worldGrid);
    }

    private async Task RenderWorldPage(int offset, string filter = null)
    {
        if (isRefreshing)
            return;

        isRefreshing = true;
        try
        {
            var (activeWorlds, error) = await worldService.GetActiveWorlds(
                offset,
                PAGE_SIZE,
                filter
            );

            logger.Log(
                $"[WorldFeed] Fetched worlds with offset {offset}, filter '{filter}'. "
                    + $"Received {activeWorlds?.Count ?? 0} worlds. Error: {error}"
            );

            if (!string.IsNullOrEmpty(error))
            {
                logger.Log(
                    $"[WorldFeed] Error fetching worlds: {error}",
                    this,
                    Logging.LogType.Error
                );
                if (!hasRenderedOnce)
                    RenderWorlds(new List<ActiveWorldData>(), null);
                return;
            }

            if (activeWorlds == null || activeWorlds.Count == 0)
            {
                if (offset > 0)
                {
                    maxPageOffset = offset - PAGE_SIZE;
                    currentOffset = maxPageOffset;
                    isRefreshing = false;
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

            var badgeStates = new Dictionary<string, ZoneStatusBadgeController.State>();
            var zoneCounts = new Dictionary<string, ZoneCount>();
            try
            {
                var (_, worldsWithZones, pageError) = await worldService.GetWorldPage(
                    offset,
                    PAGE_SIZE,
                    filter
                );

                if (string.IsNullOrEmpty(pageError) && worldsWithZones != null)
                {
                    foreach (var w in worldsWithZones)
                    {
                        badgeStates[w.id] = zoneStatusBadge.Evaluate(w.zones);

                        int total = w.zones?.Count ?? 0;
                        int online = 0;
                        if (w.zones != null)
                        {
                            foreach (var z in w.zones)
                                if (z.is_online)
                                    online++;
                        }

                        zoneCounts[w.id] = new ZoneCount { online = online, total = total };
                    }
                }
                else if (!string.IsNullOrEmpty(pageError))
                {
                    logger.Log(
                        $"[WorldFeed] Error fetching zone metadata for badges: {pageError}",
                        this,
                        Logging.LogType.Warning
                    );
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

            cachedWorlds = activeWorlds;
            cachedBadgeStates = badgeStates;
            cachedZoneCounts = zoneCounts;
            hasRenderedOnce = true;
            currentFilter = filter;

            RenderWorlds(cachedWorlds, cachedBadgeStates);
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private void RenderWorlds(
        List<ActiveWorldData> activeWorlds,
        Dictionary<string, ZoneStatusBadgeController.State> badgeStates
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

        if (worldGrid == null)
        {
            listOfWorlds.Clear();
            worldGrid = new VisualElement { name = "WorldGrid" };
            worldGrid.AddToClassList("categoryList");
            listOfWorlds.Add(worldGrid);
        }

        worldGrid.Clear();

        if (activeWorlds.Count == 0)
        {
            var noResults = new Label("No active worlds found");
            noResults.AddToClassList("noResultsMessage");
            worldGrid.Add(noResults);
            SetPaginationVisible(false);
            return;
        }

        foreach (var activeWorld in activeWorlds)
        {
            var state = ZoneStatusBadgeController.State.Offline;
            if (badgeStates != null && activeWorld.worldData?.worldId != null)
                badgeStates.TryGetValue(activeWorld.worldData.worldId, out state);

            var element = CreateWorldElement(activeWorld, state);
            if (element != null)
                worldGrid.Add(element);
        }

        bool showPagination = activeWorlds.Count >= PAGE_SIZE || currentOffset > 0;
        SetPaginationVisible(showPagination);
    }

    private void OnRefreshButtonClicked()
    {
        savedScrollOffset = Vector2.zero;
        if (listOfWorlds != null)
            listOfWorlds.scrollOffset = Vector2.zero;

        _ = RenderWorldPage(currentOffset, currentFilter);
    }

    private VisualElement CreateWorldElement(
        ActiveWorldData activeWorld,
        ZoneStatusBadgeController.State badgeState
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

        var statusBadge = zoneStatusBadge.Create(badgeState);
        statusBadge.name = "StatusBadge";

        element.Add(label);
        element.Add(statusBadge);
        element.AddManipulator(new Clickable(() => ShowWorldInfo(activeWorld)));

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
            var confirmPopup = Instantiate(confirmPopupPrefab);
            var dialogController = confirmPopup.GetComponent<ConfirmPopupController>();
            dialogController.Show(
                title: "Select World",
                question: $"Are you sure you want to enter this world?",
                onConfirm: async () =>
                {
                    try
                    {
                        worldSelector.SetSelectedWorldId(worldData.worldId);
                        worldSelector.SetSelectedZoneId(worldData.startingZone);
                        config.CurrentServerAddress = activeWorld.zoneAddress.ip;
                        config.CurrentServerPort = (ushort)activeWorld.zoneAddress.port;
                        SetWorldIdForServices(worldData.worldId);

                        var worldJoinToken = await playerService.IssueWorldJoinTokenAsync(
                            worldData.worldId
                        );
                        if (
                            worldJoinToken == null
                            || string.IsNullOrWhiteSpace(worldJoinToken.token_id)
                        )
                        {
                            logger.Log(
                                "[WorldFeed] Failed to issue world join token.",
                                this,
                                Logging.LogType.Error
                            );
                            return;
                        }

                        worldSelector.SetSelectedWorldJoinToken(worldJoinToken.token_id);

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
                            $"[WorldFeed] Exception entering world: {ex.Message}",
                            this,
                            Logging.LogType.Error
                        );
                    }
                },
                onCancel: () => { }
            );
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

    private void SetWorldIdForServices(string worldId) =>
        itemAssetsService?.SetCurrentWorldId(worldId);

    private void ShowWorldInfo(ActiveWorldData activeWorld)
    {
        if (activeWorld?.worldData == null)
            return;

        logger.Log(
            $"[WorldFeed] World info opened for world: {activeWorld.worldData.worldName}",
            this
        );

        var worldInfoMenuInstance = worldInfoMenuHandle?.Instance;
        if (worldInfoMenuInstance == null)
        {
            logger.Log(
                "[WorldFeed] World info menu instance is not assigned.",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        var worldInfoController = worldInfoMenuInstance.GetComponent<WorldInfoController>();
        if (worldInfoController == null)
        {
            logger.Log(
                "[WorldFeed] WorldInfoController not found on world info menu instance.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        var worldId = activeWorld.worldData.worldId;
        var status = ZoneStatusBadgeController.State.Offline;
        int onlineZones = 0;
        int totalZones = 0;

        if (!string.IsNullOrWhiteSpace(worldId))
        {
            cachedBadgeStates.TryGetValue(worldId, out status);
            if (cachedZoneCounts.TryGetValue(worldId, out var counts))
            {
                onlineZones = counts.online;
                totalZones = counts.total;
            }
        }

        worldInfoMenuInstance.SetActive(true);
        worldInfoController.SetCurrentWorld(
            activeWorld,
            status,
            onlineZones,
            totalZones,
            OnWorldSelected
        );
    }

    private void OnBackButtonClicked()
    {
        if (currentOffset < PAGE_SIZE)
        {
            logger.Log("[WorldFeed] Already on first page.", this, Logging.LogType.Warning);
            return;
        }
        currentOffset -= PAGE_SIZE;
        _ = RenderWorldPage(currentOffset, currentFilter);
    }

    private void OnForwardButtonClicked()
    {
        if (currentOffset >= maxPageOffset)
        {
            logger.Log("[WorldFeed] Already on last page.", this, Logging.LogType.Warning);
            return;
        }
        currentOffset += PAGE_SIZE;
        _ = RenderWorldPage(currentOffset, currentFilter);
    }

    private void SetPaginationVisible(bool visible)
    {
        var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (backButton != null)
            backButton.style.display = display;
        if (forwardButton != null)
            forwardButton.style.display = display;
    }
}
