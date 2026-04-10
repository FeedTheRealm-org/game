using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using FTR.Gameplay.Client.EntryPoints;
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
        worldSelector?.ClearSelectedWorldJoinToken();
        await RenderWorldPage(currentOffset, searchField?.value);
    }

    private async Task RenderWorldPage(int offset, string filter = null)
    {
        var (amount, worlds, error) = await worldService.GetWorldPage(
            offset,
            PAGE_SIZE,
            filter,
            session.APIToken
        );

        if (!string.IsNullOrEmpty(error))
        {
            logger.Log($"[WorldFeed] Error fetching worlds: {error}", this, Logging.LogType.Error);
            return;
        }

        if (worlds == null || worlds.Count == 0)
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
                RenderWorlds(new List<WorldData>());
            }

            return;
        }

        currentOffset = offset;

        if (worlds.Count < PAGE_SIZE)
            maxPageOffset = offset;

        RenderWorlds(worlds);
    }

    private void RenderWorlds(List<WorldData> worlds)
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

        if (worlds.Count == 0)
        {
            var noResults = new Label("No worlds found");
            noResults.AddToClassList("noResultsMessage");
            listOfWorlds.Add(noResults);
            SetPaginationVisible(false);
            return;
        }

        foreach (var world in worlds)
        {
            var element = CreateWorldElement(world);
            if (element != null)
                listOfWorlds.Add(element);
        }

        bool showPagination = worlds.Count >= PAGE_SIZE || currentOffset > 0;
        SetPaginationVisible(showPagination);
    }

    private VisualElement CreateWorldElement(WorldData worldData)
    {
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
        element.AddManipulator(new Clickable(() => _ = OnWorldSelected(worldData)));

        return element;
    }

    private async Task OnWorldSelected(WorldData worldData)
    {
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
            worldSelector?.SetSelectedWorldId(worldData.worldId);
            SetWorldIdForServices(worldData.worldId);

            if (playerService == null)
            {
                logger.Log(
                    "[WorldFeed] PlayerService is not assigned; cannot issue world join token.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            var worldJoinToken = await playerService.IssueWorldJoinTokenAsync(worldData.worldId);
            if (worldJoinToken == null || string.IsNullOrWhiteSpace(worldJoinToken.token_id))
            {
                logger.Log(
                    "[WorldFeed] Failed to issue world join token; aborting world join.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            worldSelector?.SetSelectedWorldJoinToken(worldJoinToken.token_id);

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
