using UnityEngine;

namespace Systems {
  /// <summary>
  /// Singleton ScriptableObject to hold world data between scene transitions.
  /// </summary>
  [CreateAssetMenu(fileName = "WorldLoader", menuName = "Scriptable Objects/Systems/WorldLoader")]
  public class WorldLoader : ScriptableObject {
    [Header("Current World Data")]
    [TextArea(5, 20)]
    public string worldDataJson;

    [Header("World Info")]
    public string worldId;
    public string worldName;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    /// <summary>
    /// Store world data for loading in the next scene.
    /// </summary>
    public void SetWorldData(string id, string name, string jsonData) {
      worldId = id;
      worldName = name;
      worldDataJson = jsonData;

      if (logger != null) {
        logger.Log($"World data set: {name} (ID: {id})", this);
      }
    }

    /// <summary>
    /// Get the parsed world data.
    /// </summary>
    public Models.WorldData GetWorldData() {
      if (string.IsNullOrEmpty(worldDataJson)) {
        if (logger != null) {
          logger.Log("No world data available to load", this, Logging.LogType.Warning);
        }
        return null;
      }

      try {
        var worldData = JsonUtility.FromJson<Models.WorldData>(worldDataJson);
        if (logger != null) {
          logger.Log($"Parsed world data: {worldData.worldName}, Assets: {worldData.objectPlacementData?.Count ?? 0}", this);
        }
        return worldData;
      } catch (System.Exception ex) {
        if (logger != null) {
          logger.Log($"Error parsing world data: {ex.Message}", this, Logging.LogType.Error);
        }
        return null;
      }
    }

    /// <summary>
    /// Clear the stored world data.
    /// </summary>
    public void Clear() {
      worldId = "";
      worldName = "";
      worldDataJson = "";

      if (logger != null) {
        logger.Log("World data cleared", this);
      }
    }
  }
}
