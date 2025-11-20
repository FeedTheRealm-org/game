using UnityEngine;
using System.Collections.Generic;

namespace Worlds {
  [System.Serializable]
  public class Category {
    public string name;
    public List<string> worlds = new List<string>();
  }

  [CreateAssetMenu(fileName = "Worlds", menuName = "Scriptable Objects/Worlds")]
  public class Worlds : ScriptableObject {
    public const string NULL_CATEGORY_NAME = "Uncategorized";

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

    public bool addWorldToCategory(string categoryName, string worldName) {
      if (string.IsNullOrWhiteSpace(categoryName) || string.IsNullOrWhiteSpace(worldName)) {
        logger.Log($"Invalid category or world name: {categoryName}, {worldName}", this, Logging.LogType.Error);
        return false;
      }
      var category = categories.Find(c => c.name == categoryName);
      if (category == null) {
        createACategory(categoryName);
        category = categories.Find(c => c.name == categoryName);
      }
      if (category.worlds.Contains(worldName)) {
        logger.Log($"World already exists in category: {worldName} in {categoryName}", this, Logging.LogType.Warning);
        return false;
      }
      category.worlds.Add(worldName);
      logger.Log($"World added successfully: {worldName} to {categoryName}", this);
      return true;
    }

    public List<Category> GetCategoryObjects() {
      return new List<Category>(categories);
    }
  }
}