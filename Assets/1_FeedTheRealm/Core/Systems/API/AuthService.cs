using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace API {
    public class AuthService : MonoBehaviour {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        public string Hostname;

        [SerializeField]
        public int Port;

        [System.Serializable]
        class LoginPayload { public string email; public string password; }

        [System.Serializable]
        class LoginResponse { public string message; public string token; }

        [System.Serializable]
        class SignUpResponse { public string message; public string email; }

        [System.Serializable]
        class ErrorResponse { public string error; }

        public IEnumerator Login(string email, string password, System.Action<string> handler) {
            var url = $"http://{Hostname}:{Port}/auth/login";
            var payload = new LoginPayload { email = email, password = password };
            var json = JsonUtility.ToJson(payload);

            var uwr = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
                var res = string.IsNullOrEmpty(responseText) ? null : JsonUtility.FromJson<ErrorResponse>(responseText);
                logger.Log($"Login error: {(res != null ? res.error : responseText)} - {responseText}", this, Logging.LogType.Error);
                handler?.Invoke("");
            } else {
                var res = JsonUtility.FromJson<LoginResponse>(responseText);
                logger.Log($"Login response: {responseText}", this);
                logger.Log($"Login successful: {res.token}", this);
                handler?.Invoke(!string.IsNullOrEmpty(res.token) ? res.token : "");
            }
        }

        public IEnumerator SignUp(string email, string password, System.Action<bool> handler) {
            var url = $"http://{Hostname}:{Port}/auth/signup";
            var payload = new LoginPayload { email = email, password = password };
            var json = JsonUtility.ToJson(payload);

            var uwr = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
                var res = string.IsNullOrEmpty(responseText) ? null : JsonUtility.FromJson<ErrorResponse>(responseText);
                logger.Log($"SignUp error: {(res != null ? res.error : responseText)} - {responseText}", this, Logging.LogType.Error);
                handler?.Invoke(false);
            } else {
                var res = JsonUtility.FromJson<SignUpResponse>(responseText);
                logger.Log($"SignUp response: {responseText}", this);
                logger.Log($"SignUp successful: {res.email}", this);
                handler?.Invoke(res.email == email);
            }
        }
    }
}
