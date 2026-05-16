using System.Text;
using Cysharp.Threading.Tasks;
using Session;
using UnityEngine;
using UnityEngine.Networking;

namespace API
{
    [System.Serializable]
    public class UpdateStatusRequest
    {
        public bool is_online;
    }

    public class OrchestratorServiceException : System.Exception
    {
        public OrchestratorServiceException(string message)
            : base(message) { }
    }

    /// <summary>
    /// Service to update orchestrator zone status.
    /// Handles online/offline status updates for zones in the orchestrator system.
    /// Route: PUT /world/orchestrator/{id}/zones/{zone_id}/status
    /// </summary>
    [CreateAssetMenu(
        fileName = "OrchestratorService",
        menuName = "Scriptable Objects/API/OrchestratorService"
    )]
    public class OrchestratorService : ScriptableObject
    {
        [SerializeField]
        private ApiConfig apiConfig;

        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private Logging.Logger logger;

        /// <summary>
        /// Updates the online/offline status of a zone within an orchestrator.
        /// </summary>
        /// <param name="id">The orchestrator ID.</param>
        /// <param name="zoneId">The zone ID whose status will be updated.</param>
        /// <param name="isOnline">Whether the zone should be set as online.</param>
        public async UniTask UpdateZoneStatus(string id, int zoneId, bool isOnline)
        {
            string url =
                $"{apiConfig.Hostname}:{apiConfig.Port}/world/orchestrator/{id}/zones/{zoneId}/status";
            string body = JsonUtility.ToJson(new UpdateStatusRequest { is_online = isOnline });

            using var request = new UnityWebRequest(url, "PUT");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {session.AccessToken}");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                logger.Log(
                    $"Failed to update zone status for orchestrator '{id}', zone '{zoneId}': {request.error}",
                    Logging.LogType.Error
                );
                throw new OrchestratorServiceException(
                    $"Failed to update zone status: {request.error}"
                );
            }

            logger.Log(
                $"Zone status updated successfully for orchestrator '{id}', zone '{zoneId}'."
            );
        }
    }
}
