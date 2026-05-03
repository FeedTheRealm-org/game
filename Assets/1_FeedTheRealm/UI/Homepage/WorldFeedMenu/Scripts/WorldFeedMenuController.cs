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
                RenderWorlds(new List<ActiveWorldData>());
            }
            return;
        }

        currentOffset = offset;

        if (activeWorlds.Count < PAGE_SIZE)
            maxPageOffset = offset;

        RenderWorlds(activeWorlds);
    }

    private void RenderWorlds(List<ActiveWorldData> activeWorlds)
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
            var element = CreateWorldElement(activeWorld);
            if (element != null)
                listOfWorlds.Add(element);
        }

        bool showPagination = activeWorlds.Count >= PAGE_SIZE || currentOffset > 0;
        SetPaginationVisible(showPagination);
    }

    private VisualElement CreateWorldElement(ActiveWorldData activeWorld)
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

        var aboutButton = new Button();
        aboutButton.AddToClassList("aboutButton");
        aboutButton.name = "AboutButton";
        aboutButton.text = "i";
        aboutButton.clicked += () => OnClickAboutWorld(worldData);
        aboutButton.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

        element.Add(label);
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
}
