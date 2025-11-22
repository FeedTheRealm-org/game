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

    private VisualElement ui;
    private TextField searchField;
    private Button backButton;
    private Button forwardButton;
    private int currentOffset = 0;
    private int maxPageOffset = int.MaxValue;
    private const int PAGE_SIZE = 20;
    private readonly List<Worlds.Category> allCategories = new List<Worlds.Category>();

    private void Awake() {
        ui = GetComponent<UIDocument>().rootVisualElement;

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

            foreach (var world in worlds) {
                listOfWorlds.addWorldToCategory(Worlds.Worlds.NULL_CATEGORY_NAME, world.name);
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
}
