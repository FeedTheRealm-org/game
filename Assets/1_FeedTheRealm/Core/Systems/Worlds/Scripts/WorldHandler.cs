using System.Collections.Generic;
using UnityEngine;

namespace Worlds
{
    [System.Serializable]
    public class Category
    {
        public string name;
        public string id;
        public List<Models.WorldMetadata> worlds = new();
    }

    [CreateAssetMenu(fileName = "WorldHandler", menuName = "Scriptable Objects/World/WorldHandler")]
    public class WorldHandler : ScriptableObject
    {
        public const string NULL_CATEGORY_NAME = "Uncategorized";

        public Models.WorldMetadata selectedWorld = null;

        [Header("World Categories")]
        [SerializeField]
        private List<Category> categories = new List<Category>();

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        public bool createACategory(string categoryName)
        {
            logger.Log($"Creating category: {categoryName}", this);
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                logger.Log($"Invalid category name: {categoryName}", this, Logging.LogType.Error);
                return false;
            }
            if (categories.Exists(c => c.name == categoryName))
            {
                logger.Log(
                    $"Category already exists: {categoryName}",
                    this,
                    Logging.LogType.Warning
                );
                return false;
            }
            categories.Add(new Category { name = categoryName });
            logger.Log($"Category created successfully: {categoryName}", this);
            return true;
        }

        public bool addWorldToCategory(string categoryName, Models.WorldMetadata world)
        {
            if (
                string.IsNullOrWhiteSpace(categoryName)
                || world == null
                || string.IsNullOrWhiteSpace(world.name)
            )
            {
                logger.Log(
                    $"Invalid category or world name: {categoryName}, {world?.name}",
                    this,
                    Logging.LogType.Error
                );
                return false;
            }
            var category = categories.Find(c => c.name == categoryName);
            if (category == null)
            {
                createACategory(categoryName);
                category = categories.Find(c => c.name == categoryName);
            }
            if (category.worlds.Contains(world))
            {
                logger.Log(
                    $"World already exists in category: {world.name} in {categoryName}",
                    this,
                    Logging.LogType.Warning
                );
                return false;
            }
            category.worlds.Add(world);
            logger.Log($"World added successfully: {world.name} to {categoryName}", this);
            return true;
        }

        public List<Category> GetCategoryObjects()
        {
            return new List<Category>(categories);
        }

        public void Clear()
        {
            categories.Clear();
            logger.Log("Cleared all categories and worlds.", this);
        }

        public Models.WorldMetadata GetSelectedWorld()
        {
            return selectedWorld;
        }

        public void SetSelectedWorld(Models.WorldMetadata world)
        {
            selectedWorld = world;

            // Ensure WorldData.worldName is aligned with the selected world's name
            if (selectedWorld != null && selectedWorld.data != null)
            {
                string baseName = selectedWorld.name;
                if (!string.IsNullOrWhiteSpace(baseName))
                {
                    int dotIndex = baseName.IndexOf('.');
                    if (dotIndex > 0)
                    {
                        baseName = baseName.Substring(0, dotIndex);
                    }
                }

                selectedWorld.data.worldName = baseName;
            }
        }
    }
}
