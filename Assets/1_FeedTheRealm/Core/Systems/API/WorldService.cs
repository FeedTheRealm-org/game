using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace API {
  /// <summary>
  /// Service to manage worlds data.
  /// </summary>
  [CreateAssetMenu(fileName = "WorldService", menuName = "Scriptable Objects/API/WorldService")]
  public class DEPRECATEDWorldService : ScriptableObject {
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
    /// Get a page of worlds from the server.
    /// </summary>
    public IEnumerator GetWorldPage(int offset, int limit, string filter, System.Action<int, List<WorldsData>, string> handler) {
      var url = $"http://{Hostname}:{Port}/world?offset={offset}&limit={limit}";
      if (filter != null) {
        filter = filter.Trim();
        url = $"{url}&filter={UnityWebRequest.EscapeURL(filter)}";
      }
      logger.Log($"Fetching worlds from URL: {url}", this);

      var uwr = UnityWebRequest.Get(url);

      uwr.SetRequestHeader("Content-Type", "application/json");
      uwr.SetRequestHeader("Authorization", $"Bearer {session?.APIToken}");
      logger.Log($"Using API Token: {session?.APIToken}", this);

      yield return uwr.SendWebRequest();

      var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;
      logger.Log($"Worlds response text: {responseText}", this);
      if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
        var res = string.IsNullOrEmpty(responseText) ? null : JsonUtility.FromJson<ErrorResponse>(responseText);
        logger.Log($"WorldInfo error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}", this, Logging.LogType.Error);
        handler?.Invoke(0, null, res?.detail ?? responseText);
      } else {
        var res = string.IsNullOrEmpty(responseText) ? null : JsonUtility.FromJson<DataEnvelope<WorldInfoResponse>>(responseText);
        var amount = res?.data?.amount ?? 0;
        var worlds = res?.data?.worlds ?? new List<WorldsData>();
        logger.Log($"WorldInfo response: {responseText}", this);
        handler?.Invoke(amount, worlds, "");
      }
    }
  }
}
