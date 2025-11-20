using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace API {
  /// <summary>
  /// Service to manage worlds data.
  /// </summary>
  [CreateAssetMenu(fileName = "WorldService", menuName = "Scriptable Objects/API/WorldService")]
  public class WorldService : ScriptableObject {
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
    public IEnumerator GetWorldPage(int offset, int limit, System.Action<int, List<WorldsData>, string> handler) {
      var url = $"http://{Hostname}:{Port}/worlds?offset={offset}&limit={limit}";

      var uwr = new UnityWebRequest(url, "GET");

      uwr.SetRequestHeader("Content-Type", "application/json");
      uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

      yield return uwr.SendWebRequest();

      var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

      if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
        var res = string.IsNullOrEmpty(responseText) ? null : JsonUtility.FromJson<ErrorResponse>(responseText);
        logger.Log($"CharacterInfo error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}", this, Logging.LogType.Error);
        handler?.Invoke(0, null, res.detail);
      } else {
        var res = JsonUtility.FromJson<DataEnvelope<WorldInfoResponse>>(responseText);
        logger.Log($"CharacterInfo response: {responseText}", this);
        handler?.Invoke(res.data.amount, res.data.worlds, "");
      }
    }
  }
}
