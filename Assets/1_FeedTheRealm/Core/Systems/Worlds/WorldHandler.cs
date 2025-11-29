using UnityEngine;
using System.Collections.Generic;

namespace Worlds {
  [System.Serializable]
  public class Category {
    public string name;
    public string id;
    public List<Models.WorldData> worlds = new();
  }

  [CreateAssetMenu(fileName = "WorldHandler", menuName = "Scriptable Objects/World/WorldHandler")]
  public class WorldHandler : ScriptableObject {
    public const string NULL_CATEGORY_NAME = "Uncategorized";

    public Models.WorldData selectedWorldId = null;

    [Header("World Categories")]
    [SerializeField]
    private List<Category> categories = new List<Category>();

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    public bool createACategory(string categoryName) {
      logger.Log($"Creating category: {categoryName}", this);
      if (string.IsNullOrWhiteSpace(categoryName)) {
        logger.Log($"Invalid category name: {categoryName}", this, Logging.LogType.Error);
        return false;
      }
      if (categories.Exists(c => c.name == categoryName)) {
        logger.Log($"Category already exists: {categoryName}", this, Logging.LogType.Warning);
        return false;
      }
      categories.Add(new Category { name = categoryName });
      logger.Log($"Category created successfully: {categoryName}", this);
      return true;
    }

    public bool addWorldToCategory(string categoryName, Models.WorldData worldData) {

      if (string.IsNullOrWhiteSpace(categoryName) || worldData == null || string.IsNullOrWhiteSpace(worldData.worldName)) {
        logger.Log($"Invalid category or world name: {categoryName}, {worldData?.worldName}", this, Logging.LogType.Error);
        return false;
      }
      var category = categories.Find(c => c.name == categoryName);
      if (category == null) {
        createACategory(categoryName);
        category = categories.Find(c => c.name == categoryName);
      }
      if (category.worlds.Contains(worldData)) {
        logger.Log($"World already exists in category: {worldData.worldName} in {categoryName}", this, Logging.LogType.Warning);
        return false;
      }
      category.worlds.Add(worldData);
      logger.Log($"World added successfully: {worldData.worldName} to {categoryName}", this);
      return true;
    }

    public List<Category> GetCategoryObjects() {
      return new List<Category>(categories);
    }

    public void Clear() {
      categories.Clear();
      logger.Log("Cleared all categories and worlds.", this);
    }

    public Models.WorldData GetSelectedWorldId() {
      return selectedWorldId;
    }

    public void SetSelectedWorldId(Models.WorldData worldData) {
      selectedWorldId = worldData;
    }


  }
}