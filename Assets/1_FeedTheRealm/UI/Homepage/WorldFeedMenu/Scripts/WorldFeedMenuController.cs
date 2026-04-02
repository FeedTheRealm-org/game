using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private SceneReference worldScene;

    [SerializeField]
    private WorldSelector worldSelector;
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

        backButton.clicked += OnBackButtonClicked;
        forwardButton.clicked += OnForwardButtonClicked;
    }

    private async void OnEnable()
    {
        await RenderWorldPage(currentOffset);
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

        if (worlds.Count < PAGE_SIZE)
            maxPageOffset = offset;

        RenderWorlds(worlds);
    }

    private void RenderWorlds(List<WorldData> worlds)
    {
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
        if (worldData == null || string.IsNullOrEmpty(worldData.worldName))
            return null;

        var element = new VisualElement();
        element.AddToClassList("worldElement");

        var label = new Label(worldData.worldName);
        label.AddToClassList("worldName");

        element.Add(label);
        element.AddManipulator(new Clickable(() => OnWorldSelected(worldData)));

        return element;
    }

    private void OnWorldSelected(WorldData worldData)
    {
        logger.Log(
            $"[WorldFeed] Selected world: {worldData.worldName}",
            this,
            Logging.LogType.Info
        );
        worldSelector.SetSelectedWorldId(worldData.worldId);
        SceneManager.LoadScene(worldScene.SceneName);
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
