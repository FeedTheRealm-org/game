using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FTR.Gameplay.Client.EntryPoints;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WorldFeedMenuController : MonoBehaviour, IMainMenuController
{
    //[SerializeField]
    //private Worlds.WorldHandler worldHandler;

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
    private GameObject worldInfoHUD;

    [SerializeField]
    private WorldSelector worldSelector;

    [SerializeField]
    private API.ItemAssetsService itemAssetsService;

    public event Action OnNavigateToWorld;

    private VisualElement ui;
    private TextField searchField;
    private Button backButton;
    private Button forwardButton;
    private int currentOffset = 0;
    private int maxPageOffset = int.MaxValue;
    private const int PAGE_SIZE = 20;

    //private readonly List<Worlds.Category> allCategories = new List<Worlds.Category>();
    private List<FTRShared.Runtime.Models.WorldMetadata> currentWorlds =
        new List<FTRShared.Runtime.Models.WorldMetadata>();

    private void Awake()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;

        searchField = ui.Q<TextField>("SearchField");
        if (searchField != null)
        {
            searchField.RegisterValueChangedCallback(evt =>
            {
                currentOffset = 0;
                maxPageOffset = int.MaxValue;
                RenderWorldPage(currentOffset, evt.newValue);
            });
        }
        else
        {
            logger.Log("SearchField not found in UI", this, Logging.LogType.Warning);
        }

        backButton = ui.Q<Button>("BackButton");
        if (backButton != null)
        {
            backButton.clicked += OnBackButtonClicked;
        }

        forwardButton = ui.Q<Button>("ForwardButton");
        if (forwardButton != null)
        {
            forwardButton.clicked += OnForwardButtonClicked;
        }
    }

    private void RenderWorldPage(int offset, string filter = null)
    {
        //worldHandler.Clear();

        StartCoroutine(
            worldService.GetWorldPage(
                offset,
                PAGE_SIZE,
                filter,
                session.APIToken,
                (amount, worlds, error) =>
                {
                    if (!string.IsNullOrEmpty(error))
                    {
                        logger.Log($"Error fetching worlds: {error}", this, Logging.LogType.Error);
                        return;
                    }

                    if (worlds == null || worlds.Count == 0)
                    {
                        logger.Log("No worlds received from server", this, Logging.LogType.Warning);
                        //worldHandler.Clear();

                        if (offset > 0)
                        {
                            maxPageOffset = offset - PAGE_SIZE;
                            currentOffset = maxPageOffset;
                            RenderWorldPage(currentOffset, filter);
                        }
                        else
                        {
                            maxPageOffset = 0;
                            CreateCategories();
                        }
                        return;
                    }

                    if (worlds.Count < PAGE_SIZE)
                    {
                        maxPageOffset = offset;
                    }

                    currentWorlds.Clear();
                    foreach (var world in worlds)
                    {
                        currentWorlds.Add(world);

                        //logger.Log($"Fetched world: {world.name} (ID: {world.id})",
                        // worldHandler.addWorldToCategory(
                        //     Worlds.WorldHandler.NULL_CATEGORY_NAME,
                        //     world
                        // );
                    }

                    logger.Log($"Fetched and categorized {worlds.Count} worlds.", this);
                    CreateCategories();
                }
            )
        );
    }

    private void OnEnable()
    {
        // worldHandler.createACategory(Worlds.WorldHandler.NULL_CATEGORY_NAME);
        logger.Log("Worlds OnEnable called, fetching worlds...", this);
        worldSelector?.ClearSelectedWorldJoinToken();
        RenderWorldPage(currentOffset);
    }

    private void OnBackButtonClicked()
    {
        if (currentOffset >= PAGE_SIZE)
        {
            currentOffset -= PAGE_SIZE;
            logger.Log($"Navigating to previous page, offset: {currentOffset}", this);
            RenderWorldPage(currentOffset, searchField?.value);
        }
        else
        {
            logger.Log("Already at the first page, cannot go back.", this, Logging.LogType.Warning);
        }
    }

    private void OnForwardButtonClicked()
    {
        if (currentOffset < maxPageOffset)
        {
            currentOffset += PAGE_SIZE;
            logger.Log($"Navigating to next page, offset: {currentOffset}", this);
            RenderWorldPage(currentOffset, searchField?.value);
        }
        else
        {
            logger.Log(
                "Already at the last page, cannot go forward.",
                this,
                Logging.LogType.Warning
            );
        }
    }

    private async Task OnWorldSelected(FTRShared.Runtime.Models.WorldMetadata metadata)
    {
        try
        {
            if (metadata == null || string.IsNullOrWhiteSpace(metadata.id))
            {
                logger.Log(
                    "Cannot select world: metadata is missing or world ID is empty.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            var selectedWorldId = metadata.id;

            if (worldSelector != null)
            {
                worldSelector.SetSelectedWorldId(selectedWorldId);
            }

            var worldJoinToken = await playerService.IssueWorldJoinTokenAsync(selectedWorldId);
            if (worldJoinToken == null || string.IsNullOrWhiteSpace(worldJoinToken.token_id))
            {
                logger.Log(
                    "Failed to issue world join token; aborting world join.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            worldSelector?.SetSelectedWorldJoinToken(worldJoinToken.token_id);

            if (itemAssetsService != null)
            {
                await itemAssetsService.InitializeCategoryForWorldAsync(selectedWorldId);
            }

            if (OnNavigateToWorld != null)
                OnNavigateToWorld.Invoke();
            else
                SceneManager.LoadScene(worldScene.SceneName);
        }
        catch (Exception ex)
        {
            logger.Log($"Exception in OnWorldSelected: {ex.Message}", this, Logging.LogType.Error);
        }
    }

    private void CreateCategories()
    {
        //allCategories.Clear();

        // if (worldHandler == null)
        // {
        //     logger.Log(
        //         "listOfWorlds is null - cannot load categories",
        //         this,
        //         Logging.LogType.Error
        //     );
        //     RenderCategories();
        //     return;
        // }

        //List<Worlds.Category> categories = worldHandler.GetCategoryObjects();
        // if (categories == null || categories.Count == 0)
        // {
        //     logger.Log("No categories found in listOfWorlds", this, Logging.LogType.Warning);
        //     RenderCategories();
        //     return;
        // }

        //llCategories.AddRange(categories);
        // logger.Log(
        //     $"Loaded {allCategories.Count} categories with {allCategories.Sum(c => c.worlds?.Count ?? 0)} total worlds",
        //     this
        // );

        RenderCategories();
    }

    private VisualElement CreateWorldElement(FTRShared.Runtime.Models.WorldMetadata worldData)
    {
        if (worldData == null || string.IsNullOrEmpty(worldData.name))
            return null;

        var worldElement = new VisualElement();
        worldElement.AddToClassList("worldElement");
        worldElement.name = "WorldElement";

        var worldLabel = new Label(worldData.name.Split('.')[0]);
        worldLabel.AddToClassList("worldName");
        worldLabel.name = "WorldName";

        var worldAboutButton = new Button();
        worldAboutButton.AddToClassList("aboutButton");
        worldAboutButton.name = "AboutButton";
        worldAboutButton.text = "i";
        worldAboutButton.clicked += () =>
        {
            onClickAboutWorld(worldData);
        };

        worldElement.Add(worldLabel);
        worldElement.Add(worldAboutButton);

        worldElement.AddManipulator(
            new Clickable(async () =>
            {
                await OnWorldSelected(worldData);
            })
        );

        return worldElement;
    }

    private void onClickAboutWorld(FTRShared.Runtime.Models.WorldMetadata world)
    {
        logger.Log($"About world clicked: {world.name}", this);

        if (worldInfoHUD == null)
        {
            logger.Log("WorldInfoHUD reference is not assigned.", this, Logging.LogType.Error);
            return;
        }

        var worldInfoController = worldInfoHUD.GetComponent<WorldInfoController>();
        if (worldInfoController == null)
        {
            logger.Log(
                "WorldInfoController component not found on WorldInfoHUD.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        worldInfoHUD.SetActive(true);
        worldInfoController.SetCurrentWorld(world);
    }

    private void RenderCategories()
    {
        var rootContainer = ui.Q<VisualElement>("ListOfWorlds") ?? ui;
        rootContainer.Clear();

        int totalCategories = 0;
        int totalWorlds = currentWorlds.Count;

        // foreach (var category in allCategories)
        // {
        //     if (category == null || category.worlds == null || category.worlds.Count == 0)
        //         continue;

        //     totalCategories++;
        //     totalWorlds += category.worlds.Count;
        //     rootContainer.Add(CreateCategoryContainer(category, category.worlds));
        // }

        rootContainer.Add(CreateCategoryContainer(currentWorlds));

        if (totalWorlds == 0)
        {
            rootContainer.Add(CreateNoResultsMessage());
            logger.Log("No worlds to display", this, Logging.LogType.Warning);
            if (backButton != null)
                backButton.style.display = DisplayStyle.None;
            if (forwardButton != null)
                forwardButton.style.display = DisplayStyle.None;
        }
        else if (totalWorlds < PAGE_SIZE && currentOffset == 0)
        {
            logger.Log(
                $"Rendered {totalCategories} categories with {totalWorlds} worlds (less than page size, hiding pagination)",
                this
            );
            if (backButton != null)
                backButton.style.display = DisplayStyle.None;
            if (forwardButton != null)
                forwardButton.style.display = DisplayStyle.None;
        }
        else
        {
            logger.Log($"Rendered {totalCategories} categories with {totalWorlds} worlds", this);
            if (backButton != null)
                backButton.style.display = DisplayStyle.Flex;
            if (forwardButton != null)
                forwardButton.style.display = DisplayStyle.Flex;
        }
    }

    private Label CreateNoResultsMessage()
    {
        var noResultsLabel = new Label("No worlds found");
        noResultsLabel.AddToClassList("noResultsMessage");
        return noResultsLabel;
    }

    private VisualElement CreateCategoryContainer(
        //Worlds.Category category,
        List<FTRShared.Runtime.Models.WorldMetadata> worlds
    )
    {
        var categoryContainer = new VisualElement();
        categoryContainer.AddToClassList("categoryList");

        //var nameLabel = new Label(category.name);
        //nameLabel.AddToClassList("categoryName");
        //categoryContainer.Add(nameLabel);

        foreach (var world in worlds)
        {
            var worldElement = CreateWorldElement(world);
            if (worldElement != null)
            {
                categoryContainer.Add(worldElement);
            }
        }

        return categoryContainer;
    }
}
