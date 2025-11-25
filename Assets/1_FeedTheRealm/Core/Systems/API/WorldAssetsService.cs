using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.IO.Compression;

namespace API {
  /// <summary>
  /// Service to download and manage world assets (models).
  /// </summary>
  [CreateAssetMenu(fileName = "WorldAssetsService", menuName = "Scriptable Objects/API/WorldAssetsService")]
  public class WorldAssetsService : ScriptableObject {
    [Header("Server settings")]
    [SerializeField]
    public string Hostname;

    [SerializeField]
    public int Port;

    [Header("Session settings")]
    [SerializeField]
    private Session.Session session;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    /// <summary>
    /// Download world models as a ZIP file and extract to Resources folder.
    /// </summary>
    public IEnumerator DownloadWorldModels(string worldId, System.Action<bool, string> handler) {
      var url = $"http://{Hostname}:{Port}/assets/models/{worldId}";
      logger.Log($"Downloading world models from: {url}", this);

      var uwr = UnityWebRequest.Get(url);
      uwr.SetRequestHeader("Authorization", $"Bearer {session?.APIToken}");

      yield return uwr.SendWebRequest();

      if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
        var error = uwr.error ?? "Unknown error";
        logger.Log($"Error downloading world models: {error}", this, Logging.LogType.Error);
        handler?.Invoke(false, error);
        yield break;
      }

      // Get the downloaded ZIP data
      byte[] zipData = uwr.downloadHandler.data;
      if (zipData == null || zipData.Length == 0) {
        logger.Log("Downloaded ZIP data is empty", this, Logging.LogType.Error);
        handler?.Invoke(false, "Empty ZIP file");
        yield break;
      }

      logger.Log($"Downloaded {zipData.Length} bytes", this);

      // Extract ZIP to Resources folder
      string extractPath = Path.Combine(Application.dataPath, "Resources", "WorldModels", worldId);

      try {
        // Create directory if it doesn't exist
        if (!Directory.Exists(extractPath)) {
          Directory.CreateDirectory(extractPath);
        }

        // Save ZIP temporarily
        string tempZipPath = Path.Combine(Application.temporaryCachePath, $"world_{worldId}.zip");
        File.WriteAllBytes(tempZipPath, zipData);
        logger.Log($"Saved temporary ZIP to: {tempZipPath}", this);

        // Extract ZIP
        ZipFile.ExtractToDirectory(tempZipPath, extractPath, true);
        logger.Log($"Extracted models to: {extractPath}", this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        // Clean up temp file
        File.Delete(tempZipPath);

        handler?.Invoke(true, "");
      } catch (System.Exception ex) {
        logger.Log($"Error extracting world models: {ex.Message}", this, Logging.LogType.Error);
        handler?.Invoke(false, ex.Message);
      }
    }

    /// <summary>
    /// Check if models for a world are already downloaded.
    /// </summary>
    public bool AreModelsDownloaded(string worldId) {
      string extractPath = Path.Combine(Application.dataPath, "Resources", "WorldModels", worldId);
      bool exists = Directory.Exists(extractPath) && Directory.GetFiles(extractPath).Length > 0;

      if (logger != null) {
        logger.Log($"Models for world {worldId} {(exists ? "exist" : "do not exist")} at {extractPath}", this);
      }

      return exists;
    }
  }
}
