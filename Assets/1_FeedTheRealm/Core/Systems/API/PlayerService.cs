using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace API
{
    /// <summary>
    /// Service to manage player character information.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerService", menuName = "Scriptable Objects/API/PlayerService")]
    public class PlayerService : ScriptableObject
    {
        [Header("API Config")]
        [SerializeField]
        private ApiConfig apiConfig;

        [Header("Session settings")]
        [SerializeField]
        private Session.Session session;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        private string GetBaseUrl() =>
            $"http://{apiConfig.Hostname}:{apiConfig.Port}/player/character";

        /// <summary>
        /// Update the character information such as name and bio.
        /// </summary>
        public IEnumerator PatchCharacterInfo(
            PatchCharacterInfoRequest payload,
            System.Action<CharacterInfoResponse, string> handler
        )
        {
            var url = GetBaseUrl();
            var json = JsonConvert.SerializeObject(payload);

            var uwr = new UnityWebRequest(url, "PATCH");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                var res = string.IsNullOrEmpty(responseText)
                    ? null
                    : JsonConvert.DeserializeObject<ErrorResponse>(responseText);
                logger.Log(
                    $"CharacterInfo error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}",
                    this,
                    Logging.LogType.Error
                );
                handler?.Invoke(null, res.detail);
            }
            else
            {
                var res = JsonConvert.DeserializeObject<DataEnvelope<CharacterInfoResponse>>(
                    responseText
                );
                logger.Log($"CharacterInfo response: {responseText}", this);
                handler?.Invoke(res.data, "");
            }
        }

        /// <summary>
        /// Retrieve the character information such as name and bio for a given user.
        /// If no userID is provided it retrieves the currently logged in userID.
        /// </summary>
        public IEnumerator GetCharacterInfo(
            System.Action<CharacterInfoResponse, string> handler,
            string UserID = null
        )
        {
            var url = $"{GetBaseUrl()}/{(UserID ?? session.UserId)}";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                var res = string.IsNullOrEmpty(responseText)
                    ? null
                    : JsonConvert.DeserializeObject<ErrorResponse>(responseText);
                logger.Log(
                    $"CharacterInfo error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}",
                    this,
                    Logging.LogType.Warning
                );
                handler?.Invoke(null, res.detail);
            }
            else
            {
                var res = JsonConvert.DeserializeObject<DataEnvelope<CharacterInfoResponse>>(
                    responseText
                );
                logger.Log($"CharacterInfo response: {responseText}", this);
                handler?.Invoke(res.data, "");
            }
        }

        /// <summary>
        /// Retrieve the character information such as name and bio for a given user asynchronously.
        /// If no userID is provided it retrieves the currently logged in userID.
        /// </summary>
        public async Task<CharacterInfoResponse> GetCharacterInfoAsync(string UserID = null)
        {
            var url = $"{GetBaseUrl()}/{(UserID ?? session.UserId)}";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

            await uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                var res = string.IsNullOrEmpty(responseText)
                    ? null
                    : JsonConvert.DeserializeObject<ErrorResponse>(responseText);
                logger.Log(
                    $"GetCharacterInfo error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}",
                    this,
                    Logging.LogType.Warning
                );
                return null;
            }
            else
            {
                var res = JsonConvert.DeserializeObject<DataEnvelope<CharacterInfoResponse>>(
                    responseText
                );
                logger.Log($"GetCharacterInfo response: {responseText}", this);
                return res.data;
            }
        }

        /// <summary>
        /// Update the character information such as name and bio asynchronously.
        /// </summary>
        public async Task<CharacterInfoResponse> PatchCharacterInfoAsync(
            PatchCharacterInfoRequest payload
        )
        {
            var url = GetBaseUrl();
            var json = JsonConvert.SerializeObject(payload);

            var uwr = new UnityWebRequest(url, "PATCH");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

            await uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                var res = string.IsNullOrEmpty(responseText)
                    ? null
                    : JsonConvert.DeserializeObject<ErrorResponse>(responseText);
                logger.Log(
                    $"PatchCharacterInfo error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}",
                    this,
                    Logging.LogType.Error
                );
                return null;
            }
            else
            {
                var res = JsonConvert.DeserializeObject<DataEnvelope<CharacterInfoResponse>>(
                    responseText
                );
                logger.Log($"PatchCharacterInfo response: {responseText}", this);
                return res.data;
            }
        }
    }
}
