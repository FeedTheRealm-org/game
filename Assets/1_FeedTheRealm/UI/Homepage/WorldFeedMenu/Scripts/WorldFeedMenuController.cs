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
    private readonly List<Worlds.Category> allCategories = new List<Worlds.Category>();

    private void Awake() {
        ui = GetComponent<UIDocument>().rootVisualElement;

        searchField = ui.Q<TextField>("SearchField");
        if (searchField != null) {
            searchField.RegisterValueChangedCallback(evt => RenderCategories(evt.newValue));
        } else {
            logger.Log("SearchField not found in UI", this, Logging.LogType.Warning);
        }
    }

    private void OnEnable() {
        listOfWorlds.createACategory(Worlds.Worlds.NULL_CATEGORY_NAME);
        logger.Log("Worlds OnEnable called, fetching worlds...", this);

        StartCoroutine(worldService.GetWorldPage(0, 10, (amount, worlds, error) => {
            if (!string.IsNullOrEmpty(error)) {
                logger.Log($"Error fetching worlds: {error}", this, Logging.LogType.Error);
                return;
            }

            foreach (var world in worlds) {
                listOfWorlds.addWorldToCategory(Worlds.Worlds.NULL_CATEGORY_NAME, world.name);
            }

            logger.Log($"Fetched and categorized {worlds.Count} worlds.", this);
            CreateCategories();
        }));
    }

    private void CreateCategories() {
        allCategories.Clear();

        if (listOfWorlds == null) {
            logger.Log("listOfWorlds is null - cannot load categories", this, Logging.LogType.Error);
            return;
        }

        List<Worlds.Category> categories = listOfWorlds.GetCategoryObjects();
        if (categories == null || categories.Count == 0) {
            logger.Log("No categories found in listOfWorlds", this, Logging.LogType.Warning);
            return;
        }

        allCategories.AddRange(categories);
        logger.Log($"Loaded {allCategories.Count} categories with {allCategories.Sum(c => c.worlds?.Count ?? 0)} total worlds", this);

        RenderCategories(searchField != null ? searchField.value : string.Empty);
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

    private void RenderCategories(string filter) {
        var rootContainer = ui.Q<VisualElement>("ListOfWorlds") ?? ui;
        rootContainer.Clear();

        var trimmedFilter = (filter ?? string.Empty).Trim();
        int totalMatchedCategories = 0;
        int totalMatchedWorlds = 0;

        foreach (var category in allCategories) {
            if (category == null) continue;

            var matchedWorlds = GetMatchedWorlds(category, trimmedFilter);
            if (matchedWorlds.Count == 0) continue;

            totalMatchedCategories++;
            totalMatchedWorlds += matchedWorlds.Count;
            rootContainer.Add(CreateCategoryContainer(category, matchedWorlds));
        }

        if (totalMatchedCategories == 0 && !string.IsNullOrEmpty(trimmedFilter)) {
            rootContainer.Add(CreateNoResultsMessage(trimmedFilter));
            logger.Log($"No results found for filter: '{trimmedFilter}'", this, Logging.LogType.Warning);
        }
    }

    private List<string> GetMatchedWorlds(Worlds.Category category, string filter) {
        if (category.worlds == null || category.worlds.Count == 0) {
            return new List<string>();
        }

        bool categoryMatches = string.IsNullOrEmpty(filter) ||
                               category.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        var matchedWorlds = new List<string>();
        foreach (var world in category.worlds) {
            if (string.IsNullOrEmpty(world)) continue;
            if (string.IsNullOrEmpty(filter) ||
                categoryMatches ||
                world.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) {
                matchedWorlds.Add(world);
            }
        }

        return matchedWorlds;
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

    private Label CreateNoResultsMessage(string filter) {
        var noResultsLabel = new Label($"No worlds or categories found matching '{filter}'");
        noResultsLabel.AddToClassList("noResultsMessage");

        return noResultsLabel;
    }
}
