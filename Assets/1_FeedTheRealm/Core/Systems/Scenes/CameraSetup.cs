using Mirror;
using Unity.Cinemachine;
using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    [Header("Camera Target")]
    [Tooltip("Optional: Specify a tag to find the player. Default is 'Player'.")]
    public string playerTag = "Player";

    [Tooltip(
        "Optional: Manually set a specific child transform of the player (like a spine/chest bone)."
    )]
    public string targetChildName = "";

    [SerializeField]
    private Logging.Logger logger;

    private Coroutine setupCoroutine;

    void Awake() { }

    void Start()
    {
        // Only setup camera on clients. If there is no active client (dedicated server
        // or not yet connected), disable this component early to avoid server-side logs.
        if (!NetworkClient.active)
        {
            logger.Log(
                "CameraSetup disabled: no active client (dedicated server or not connected)",
                this
            );
            enabled = false;
            return;
        }

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null && Camera.main == null)
        {
            cam.tag = "MainCamera";
        }

        // Wait a frame for the player to spawn
        setupCoroutine = StartCoroutine(WaitForPlayerAndSetupCamera());
    }

    private void OnDestroy()
    {
        if (setupCoroutine != null)
        {
            StopCoroutine(setupCoroutine);
            setupCoroutine = null;
        }
    }

    private System.Collections.IEnumerator WaitForPlayerAndSetupCamera()
    {
        // Wait a few frames for NetworkManager to spawn the player
        yield return new WaitForSeconds(0.5f);

        // Ensure we are still a client before attempting to find the local player.
        if (!NetworkClient.active)
        {
            logger.Log("CameraSetup aborting: client not active after wait", this);
            yield break;
        }

        SetupCinemachineCamera();
    }

    private void SetupCinemachineCamera()
    {
        // Find the local player (the one owned by this client)
        Transform playerTransform = FindLocalPlayer();

        if (playerTransform == null)
        {
            logger.Log(
                "CameraSetup: Local player not found! Make sure the player prefab has a NetworkIdentity and the player tag is set.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        // If a specific child is specified, try to find it
        Transform targetTransform = playerTransform;
        if (!string.IsNullOrEmpty(targetChildName))
        {
            Transform child = playerTransform.Find(targetChildName);
            if (child != null)
            {
                targetTransform = child;
                logger.Log($"Using child '{targetChildName}' as camera target", this);
            }
            else
            {
                logger.Log(
                    $"Child '{targetChildName}' not found, using player root instead",
                    this,
                    Logging.LogType.Warning
                );
            }
        }

        // Find FreeLook Camera in the scene
        var cinemachineCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);

        foreach (var vcam in cinemachineCameras)
        {
            // Configure the camera to follow this player
            vcam.Target.TrackingTarget = targetTransform;
            logger.Log(
                $"✓ Cinemachine camera '{vcam.gameObject.name}' tracking target set to: {targetTransform.name} on player: {playerTransform.name}",
                this
            );
        }

        // Alternative: Find by name if you have a specific camera
        GameObject freeLookObj = GameObject.Find("FreeLook Camera");
        if (freeLookObj != null)
        {
            var freeLookCam = freeLookObj.GetComponent<CinemachineCamera>();
            if (freeLookCam != null)
            {
                freeLookCam.Target.TrackingTarget = targetTransform;
                logger.Log(
                    $"✓ FreeLook Camera tracking target set to: {targetTransform.name} on player: {playerTransform.name}",
                    this
                );
            }
        }
    }

    private Transform FindLocalPlayer()
    {
        // First try to find by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            var netIdentity = playerObj.GetComponent<NetworkIdentity>();
            if (netIdentity != null && netIdentity.isLocalPlayer)
            {
                logger.Log($"Found local player by tag '{playerTag}': {playerObj.name}", this);
                return playerObj.transform;
            }
        }

        // If tag search fails, find all NetworkIdentities and get the local player
        var allNetworkIdentities = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);
        foreach (var netIdentity in allNetworkIdentities)
        {
            // In Mirror, isLocalPlayer indicates this is the local player
            if (netIdentity.isLocalPlayer)
            {
                logger.Log(
                    $"Found local player by NetworkIdentity scan: {netIdentity.gameObject.name}",
                    this
                );
                return netIdentity.transform;
            }
        }

        logger.Log("Local player not found!", this, Logging.LogType.Warning);
        return null;
    }
}
