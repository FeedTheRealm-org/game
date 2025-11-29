using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;

namespace API {
    /// <summary>
    /// Service to manage player character information.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerService", menuName = "Scriptable Objects/API/PlayerService")]
    public class PlayerService : ScriptableObject {
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
        /// Update the character information such as name and bio.
        /// </summary>
        public IEnumerator PatchCharacterInfo(PatchCharacterInfoRequest payload, System.Action<CharacterInfoResponse, string> handler) {
            var url = $"http://{Hostname}:{Port}/player/character";
            var json = JsonConvert.SerializeObject(payload);

            var uwr = new UnityWebRequest(url, "PATCH");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
                var res = string.IsNullOrEmpty(responseText) ? null : JsonConvert.DeserializeObject<ErrorResponse>(responseText);
                logger.Log($"CharacterInfo error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}", this, Logging.LogType.Error);
                handler?.Invoke(null, res.detail);
            } else {
                var res = JsonConvert.DeserializeObject<DataEnvelope<CharacterInfoResponse>>(responseText);
                logger.Log($"CharacterInfo response: {responseText}", this);
                handler?.Invoke(res.data, "");
            }
        }

        /// <summary>
        /// Retrieve the character information such as name and bio for a given user.
        /// If no userID is provided it retrieves the currently logged in userID.
        /// </summary>
        public IEnumerator GetCharacterInfo(System.Action<CharacterInfoResponse, string> handler, string UserID = null) {
            var url = $"http://{Hostname}:{Port}/player/character/{(UserID ?? session.UserId)}";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
                var res = string.IsNullOrEmpty(responseText) ? null : JsonConvert.DeserializeObject<ErrorResponse>(responseText);
                logger.Log($"CharacterInfo error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}", this, Logging.LogType.Error);
                handler?.Invoke(null, res.detail);
            } else {
                var res = JsonConvert.DeserializeObject<DataEnvelope<CharacterInfoResponse>>(responseText);
                logger.Log($"CharacterInfo response: {responseText}", this);
                handler?.Invoke(res.data, "");
            }
        }
    }
}
