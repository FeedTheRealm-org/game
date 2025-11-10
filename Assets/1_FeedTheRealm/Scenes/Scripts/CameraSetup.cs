using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

public class CameraSetup : MonoBehaviour
{
    [Header("Camera Target")]
    [Tooltip("Optional: Specify a tag to find the player. Default is 'Player'.")]
    public string playerTag = "Player";

    [Tooltip("Optional: Manually set a specific child transform of the player (like a spine/chest bone).")]
    public string targetChildName = "";

    [SerializeField] private Logging.Logger logger;
    
    private Coroutine setupCoroutine;

    void Awake()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null && Camera.main == null)
        {
            cam.tag = "MainCamera";
        }
    }

    void Start()
    {
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

        SetupCinemachineCamera();
    }

    private void SetupCinemachineCamera()
    {
        // Find the local player (the one owned by this client)
        Transform playerTransform = FindLocalPlayer();

        if (playerTransform == null)
        {
            logger.Log("CameraSetup: Local player not found! Make sure the player prefab has a NetworkObject and the player tag is set.", this, Logging.LogType.Error);
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
                logger.Log($"Child '{targetChildName}' not found, using player root instead", this, Logging.LogType.Warning);
            }
        }

        // Find FreeLook Camera in the scene
        var cinemachineCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        
        foreach (var vcam in cinemachineCameras)
        {
            // Configure the camera to follow this player
            vcam.Target.TrackingTarget = targetTransform;
            logger.Log($"✓ Cinemachine camera '{vcam.gameObject.name}' tracking target set to: {targetTransform.name} on player: {playerTransform.name}", this);
        }

        // Alternative: Find by name if you have a specific camera
        GameObject freeLookObj = GameObject.Find("FreeLook Camera");
        if (freeLookObj != null)
        {
            var freeLookCam = freeLookObj.GetComponent<CinemachineCamera>();
            if (freeLookCam != null)
            {
                freeLookCam.Target.TrackingTarget = targetTransform;
                logger.Log($"✓ FreeLook Camera tracking target set to: {targetTransform.name} on player: {playerTransform.name}", this);
            }
        }
    }

    private Transform FindLocalPlayer()
    {
        // First try to find by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            var netObj = playerObj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                logger.Log($"Found local player by tag '{playerTag}': {playerObj.name}", this);
                return playerObj.transform;
            }
        }

        // If tag search fails, find all NetworkObjects and get the local player
        var allNetworkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (var netObj in allNetworkObjects)
        {
            if (netObj.IsOwner && netObj.IsPlayerObject)
            {
                logger.Log($"Found local player by NetworkObject scan: {netObj.gameObject.name}", this);
                return netObj.transform;
            }
        }

        logger.Log("Local player not found!", this, Logging.LogType.Warning);
        return null;
    }
}