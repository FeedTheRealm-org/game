using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class WorldFeedMenuController : MonoBehaviour {
    [SerializeField]
    private Worlds.Worlds listOfWorlds;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private API.WorldService worldService;

    [SerializeField]
    private API.WorldAssetsService worldAssetsService;

    [SerializeField]
    private Systems.WorldLoader worldLoader;

    [SerializeField]
    private WorldDetailsModalController detailsModalController;

    [Header("Scene Settings")]
    [SerializeField]
    private string gameSceneName = "GameScene";

    private VisualElement ui;
    private TextField searchField;
    private Button backButton;
    private Button forwardButton;
    private int currentOffset = 0;
    private int maxPageOffset = int.MaxValue;
    private const int PAGE_SIZE = 20;
    private readonly List<Worlds.Category> allCategories = new List<Worlds.Category>();
    private readonly Dictionary<string, API.WorldsData> worldDataMap = new Dictionary<string, API.WorldsData>();

    private void Awake() {
        ui = GetComponent<UIDocument>().rootVisualElement;

        // Initialize the details modal controller
        if (detailsModalController != null) {
            detailsModalController.Initialize(ui);
        } else {
            logger.Log("WorldDetailsModalController not assigned!", this, Logging.LogType.Warning);
        }

        searchField = ui.Q<TextField>("SearchField");
        if (searchField != null) {
            searchField.RegisterValueChangedCallback(evt => {
                currentOffset = 0;
                maxPageOffset = int.MaxValue;
                RenderWorldPage(currentOffset, evt.newValue);
            });
        } else {
            logger.Log("SearchField not found in UI", this, Logging.LogType.Warning);
        }

        backButton = ui.Q<Button>("BackButton");
        if (backButton != null) {
            backButton.clicked += OnBackButtonClicked;
        }

        forwardButton = ui.Q<Button>("ForwardButton");
        if (forwardButton != null) {
            forwardButton.clicked += OnForwardButtonClicked;
        }
    }

    private void RenderWorldPage(int offset, string filter = null) {
        listOfWorlds.Clear();

        StartCoroutine(worldService.GetWorldPage(offset, PAGE_SIZE, filter, (amount, worlds, error) => {
            if (!string.IsNullOrEmpty(error)) {
                logger.Log($"Error fetching worlds: {error}", this, Logging.LogType.Error);
                return;
            }

            if (worlds == null || worlds.Count == 0) {
                logger.Log("No worlds received from server", this, Logging.LogType.Warning);
                listOfWorlds.Clear();

                if (offset > 0) {
                    maxPageOffset = offset - PAGE_SIZE;
                    currentOffset = maxPageOffset;
                    RenderWorldPage(currentOffset, filter);
                } else {
                    maxPageOffset = 0;
                    CreateCategories();
                }
                return;
            }

            if (worlds.Count < PAGE_SIZE) {
                maxPageOffset = offset;
            }

            worldDataMap.Clear();
            foreach (var world in worlds) {
                listOfWorlds.addWorldToCategory(Worlds.Worlds.NULL_CATEGORY_NAME, world.name);
                worldDataMap[world.name] = world;
            }

            logger.Log($"Fetched and categorized {worlds.Count} worlds.", this);
            CreateCategories();
        }));
    }

    private void OnEnable() {
        listOfWorlds.createACategory(Worlds.Worlds.NULL_CATEGORY_NAME);
        logger.Log("Worlds OnEnable called, fetching worlds...", this);
        RenderWorldPage(currentOffset);
    }

    private void OnBackButtonClicked() {
        if (currentOffset >= PAGE_SIZE) {
            currentOffset -= PAGE_SIZE;
            logger.Log($"Navigating to previous page, offset: {currentOffset}", this);
            RenderWorldPage(currentOffset, searchField?.value);
        } else {
            logger.Log("Already at the first page, cannot go back.", this, Logging.LogType.Warning);
        }
    }

    private void OnForwardButtonClicked() {
        if (currentOffset < maxPageOffset) {
            currentOffset += PAGE_SIZE;
            logger.Log($"Navigating to next page, offset: {currentOffset}", this);
            RenderWorldPage(currentOffset, searchField?.value);
        } else {
            logger.Log("Already at the last page, cannot go forward.", this, Logging.LogType.Warning);
        }
    }

    private void CreateCategories() {
        allCategories.Clear();

        if (listOfWorlds == null) {
            logger.Log("listOfWorlds is null - cannot load categories", this, Logging.LogType.Error);
            RenderCategories();
            return;
        }

        List<Worlds.Category> categories = listOfWorlds.GetCategoryObjects();
        if (categories == null || categories.Count == 0) {
            logger.Log("No categories found in listOfWorlds", this, Logging.LogType.Warning);
            RenderCategories();
            return;
        }

        allCategories.AddRange(categories);
        logger.Log($"Loaded {allCategories.Count} categories with {allCategories.Sum(c => c.worlds?.Count ?? 0)} total worlds", this);

        RenderCategories();
    }


    private VisualElement CreateWorldElement(string worldName) {
        if (string.IsNullOrEmpty(worldName)) return null;

        var worldElement = new VisualElement();
        worldElement.AddToClassList("worldElement");
        worldElement.name = "WorldElement";

        var worldLabel = new Label(worldName);
        worldLabel.AddToClassList("worldName");
        worldLabel.name = "WorldName";

        worldElement.Add(worldLabel);

        // Make the entire world element clickable to join
        worldElement.RegisterCallback<ClickEvent>(evt => OnWorldElementClicked(worldName));

        // Create buttons container
        var buttonsContainer = new VisualElement();
        buttonsContainer.AddToClassList("worldButtonsContainer");
        buttonsContainer.name = "ButtonsContainer";

        // Create Details button
        var detailsButton = new Button(() => OnDetailsButtonClicked(worldName));
        detailsButton.text = "Details";
        detailsButton.AddToClassList("worldButton");
        detailsButton.AddToClassList("detailsButton");
        detailsButton.name = "DetailsButton";

        buttonsContainer.Add(detailsButton);
        worldElement.Add(buttonsContainer);

        return worldElement;
    }

    private void RenderCategories() {
        var rootContainer = ui.Q<VisualElement>("ListOfWorlds") ?? ui;
        rootContainer.Clear();

        int totalCategories = 0;
        int totalWorlds = 0;

        foreach (var category in allCategories) {
            if (category == null || category.worlds == null || category.worlds.Count == 0) continue;

            totalCategories++;
            totalWorlds += category.worlds.Count;
            rootContainer.Add(CreateCategoryContainer(category, category.worlds));
        }

        if (totalWorlds == 0) {
            rootContainer.Add(CreateNoResultsMessage());
            logger.Log("No worlds to display", this, Logging.LogType.Warning);
            if (backButton != null) backButton.style.display = DisplayStyle.None;
            if (forwardButton != null) forwardButton.style.display = DisplayStyle.None;
        } else if (totalWorlds < PAGE_SIZE && currentOffset == 0) {
            logger.Log($"Rendered {totalCategories} categories with {totalWorlds} worlds (less than page size, hiding pagination)", this);
            if (backButton != null) backButton.style.display = DisplayStyle.None;
            if (forwardButton != null) forwardButton.style.display = DisplayStyle.None;
        } else {
            logger.Log($"Rendered {totalCategories} categories with {totalWorlds} worlds", this);
            if (backButton != null) backButton.style.display = DisplayStyle.Flex;
            if (forwardButton != null) forwardButton.style.display = DisplayStyle.Flex;
        }
    }

    private Label CreateNoResultsMessage() {
        var noResultsLabel = new Label("No worlds found");
        noResultsLabel.AddToClassList("noResultsMessage");
        return noResultsLabel;
    }

    private VisualElement CreateCategoryContainer(Worlds.Category category, List<string> worlds) {
        var categoryContainer = new VisualElement();
        categoryContainer.AddToClassList("categoryList");

        var nameLabel = new Label(category.name);
        nameLabel.AddToClassList("categoryName");
        categoryContainer.Add(nameLabel);

        foreach (var world in worlds) {
            var worldElement = CreateWorldElement(world);
            if (worldElement != null) {
                categoryContainer.Add(worldElement);
            }
        }

        return categoryContainer;
    }

    private void OnWorldElementClicked(string worldName) {
        if (!worldDataMap.TryGetValue(worldName, out var worldData)) {
            logger.Log($"World data not found for: {worldName}", this, Logging.LogType.Error);
            return;
        }

        logger.Log($"World element clicked for world: {worldName} (ID: {worldData.id})", this);
        StartCoroutine(LoadAndJoinWorld(worldData.id));
    }

    private void OnDetailsButtonClicked(string worldName) {
        if (!worldDataMap.TryGetValue(worldName, out var worldData)) {
            logger.Log($"World data not found for: {worldName}", this, Logging.LogType.Error);
            return;
        }

        logger.Log($"Details button clicked for world: {worldName}", this);

        if (detailsModalController != null) {
            detailsModalController.Show(worldData);
        } else {
            logger.Log("Details modal controller not available", this, Logging.LogType.Error);
        }
    }

    private System.Collections.IEnumerator LoadAndJoinWorld(string worldId) {
        logger.Log($"========== STARTING WORLD JOIN PROCESS ==========", this);
        logger.Log($"Step 1: Fetching world data for ID: {worldId}", this);

        // Step 1: Fetch world data from API
        bool worldDataFetched = false;
        API.WorldsData fetchedWorldData = null;
        string fetchError = null;

        yield return worldService.GetWorld(worldId, (worldData, error) => {
            fetchedWorldData = worldData;
            fetchError = error;
            worldDataFetched = true;
        });

        // Wait for callback
        yield return new WaitUntil(() => worldDataFetched);

        if (!string.IsNullOrEmpty(fetchError)) {
            logger.Log($"❌ Error loading world: {fetchError}", this, Logging.LogType.Error);
            yield break;
        }

        if (fetchedWorldData == null) {
            logger.Log("❌ World data is null", this, Logging.LogType.Error);
            yield break;
        }

        logger.Log($"✅ World data loaded: {fetchedWorldData.name}", this);

        // Step 2: Download world assets (models) if not already downloaded
        logger.Log($"Step 2: Checking for world assets...", this);
        if (worldAssetsService != null) {
            bool assetsDownloaded = worldAssetsService.AreModelsDownloaded(worldId);

            if (!assetsDownloaded) {
                logger.Log($"⏳ Downloading world models...", this);
                bool downloadComplete = false;
                bool downloadSuccess = false;
                string downloadError = null;

                yield return worldAssetsService.DownloadWorldModels(worldId, (success, error) => {
                    downloadSuccess = success;
                    downloadError = error;
                    downloadComplete = true;
                });

                yield return new WaitUntil(() => downloadComplete);

                if (!downloadSuccess) {
                    // 404 means the world has no custom assets uploaded yet (expected for many worlds)
                    if (downloadError != null && downloadError.Contains("404")) {
                        logger.Log($"ℹ️ No custom assets found for this world (this is normal)", this, Logging.LogType.Info);
                    } else {
                        logger.Log($"⚠️ Could not download world assets: {downloadError}", this, Logging.LogType.Warning);
                    }
                    logger.Log("Continuing with default assets only", this, Logging.LogType.Info);
                } else {
                    logger.Log($"✅ World assets downloaded successfully", this);
                }
            } else {
                logger.Log($"✅ World assets already downloaded", this);
            }
        } else {
            logger.Log("⚠️ WorldAssetsService is not assigned! Assets will not be downloaded.", this, Logging.LogType.Warning);
        }

        // Step 3: Store world data in WorldLoader for the game scene
        logger.Log($"Step 3: Storing world data in WorldLoader...", this);
        if (worldLoader != null) {
            worldLoader.SetWorldData(fetchedWorldData.id, fetchedWorldData.name, fetchedWorldData.data);
            logger.Log($"✅ World data stored in WorldLoader", this);
        } else {
            logger.Log("⚠️ WorldLoader is not assigned! World data will not persist to game scene.", this, Logging.LogType.Warning);
        }

        // Step 4: Load the game scene
        logger.Log($"Step 4: Loading game scene: {gameSceneName}", this);
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);

        logger.Log($"========== WORLD JOIN PROCESS COMPLETE ==========", this);
    }
}
