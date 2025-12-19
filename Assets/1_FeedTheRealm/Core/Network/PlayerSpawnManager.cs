using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class PlayerSpawnManager : Mirror.NetworkBehaviour {
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;

    [Header("Auto-detect Spawn Points")]
    [SerializeField] private bool autoDetectSpawnPoints = true;
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    [Header("Ground Reference")]
    [SerializeField] private Transform groundReference;
    [SerializeField] private bool adjustToGroundHeight = true;
    [SerializeField] private float heightOffset = 0f;

    [Header("Network Settings")]
    [SerializeField] private int maxPlayers = 100;

    [SerializeField] private Logging.Logger logger;

    // Store coroutine references for cleanup
    private Coroutine repositionCoroutine;
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    private void Awake() {
        if (autoDetectSpawnPoints) {
            DetectSpawnPoints();
        }
    }

    private void DetectSpawnPoints() {
        List<Transform> detectedSpawnPoints = new List<Transform>();

        foreach (Transform child in transform) {
            detectedSpawnPoints.Add(child);
        }

        GameObject[] taggedSpawnPoints = GameObject.FindGameObjectsWithTag(spawnPointTag);
        foreach (GameObject spawnObj in taggedSpawnPoints) {
            if (!detectedSpawnPoints.Contains(spawnObj.transform)) {
                detectedSpawnPoints.Add(spawnObj.transform);
            }
        }

        if (detectedSpawnPoints.Count > 0) {
            spawnPoints = detectedSpawnPoints.ToArray();
            logger.Log($"[PlayerSpawnManager] Detected {spawnPoints.Length} spawn points automatically", this);
        } else {
            logger.Log("[PlayerSpawnManager] No spawn points detected. Using default position.", this, Logging.LogType.Warning);
        }
    }

    public override void OnStartServer() {
        if (spawnPoints == null || spawnPoints.Length == 0) {
            logger.Log("[PlayerSpawnManager] No spawn points configured!", this, Logging.LogType.Error);
        } else {
            logger.Log($"[PlayerSpawnManager] Spawn Manager activated. Max players: {maxPlayers}, Spawn points: {spawnPoints.Length}", this);

            for (int i = 0; i < spawnPoints.Length; i++) {
                logger.Log($"[PlayerSpawnManager] Spawn Point {i}: Position = {spawnPoints[i].position}, Rotation = {spawnPoints[i].rotation.eulerAngles}", this);
            }
        }

        NetworkServer.OnConnectedEvent += OnClientConnectedSpawn;

        repositionCoroutine = StartCoroutine(DelayedRepositionPlayers());
    }

    private System.Collections.IEnumerator DelayedRepositionPlayers() {
        // Minimal delay to ensure NetworkIdentity is initialized
        yield return new WaitForFixedUpdate();

        RepositionExistingPlayers();
        repositionCoroutine = null;
    }

    private void RepositionExistingPlayers() {
        logger.Log($"[PlayerSpawnManager] Repositioning existing players in scene... Connected clients: {NetworkServer.connections.Count}", this);

        int playerIndex = 0;
        foreach (var conn in NetworkServer.connections.Values) {
            if (conn.identity != null) {
                logger.Log($"[PlayerSpawnManager] Found player {conn.connectionId} with identity", this);
                Coroutine coroutine = StartCoroutine(RepositionPlayerWithDelay(conn.identity, playerIndex));
                activeCoroutines.Add(coroutine);
                playerIndex++;
            } else {
                logger.Log($"[PlayerSpawnManager] Connection {conn.connectionId} has no identity yet", this, Logging.LogType.Warning);
            }
        }

        logger.Log($"[PlayerSpawnManager] Initiated repositioning for {playerIndex} player(s)", this);
    }

    private System.Collections.IEnumerator RepositionPlayerWithDelay(NetworkIdentity playerIdentity, int index) {
        logger.Log($"[PlayerSpawnManager] Starting delayed reposition for player {playerIdentity.netId}, waiting for NetworkIdentity to be ready...", this);

        yield return new WaitForFixedUpdate();

        RepositionPlayer(playerIdentity, index);

        // Remove from active coroutines list when complete
        // Note: Can't reference 'this' coroutine directly, cleanup happens in OnStopServer/OnDestroy
    }

    private void RepositionPlayer(NetworkIdentity playerIdentity, int index) {
        if (spawnPoints == null || spawnPoints.Length == 0) {
            logger.Log("[PlayerSpawnManager] No spawn points to reposition", this, Logging.LogType.Warning);
            return;
        }

        int spawnIndex = index % spawnPoints.Length;
        Vector3 newPosition = GetSpawnPositionByIndex(index);
        Quaternion newRotation = GetSpawnRotationByIndex(index);

        logger.Log($"[PlayerSpawnManager] Repositioning player {playerIdentity.netId} to spawn {spawnIndex} at {newPosition}, current position: {playerIdentity.transform.position}", this);

        // Use NetworkMovementSynchronizer if available for better synchronization
        NetworkMovementSynchronizer movementSync = playerIdentity.GetComponent<NetworkMovementSynchronizer>();
        if (movementSync != null) {
            // Set rotation directly (Teleport only handles position)
            playerIdentity.transform.rotation = newRotation;
            // Use Teleport for position to ensure proper network sync
            movementSync.Teleport(newPosition);
            logger.Log($"[PlayerSpawnManager] Player {playerIdentity.netId} repositioned using NetworkMovementSynchronizer.Teleport()", this);
        } else {
            // Fallback: Use transform directly (server has authority)
            playerIdentity.transform.SetPositionAndRotation(newPosition, newRotation);
            logger.Log($"[PlayerSpawnManager] Player {playerIdentity.netId} repositioned using transform (no NetworkMovementSynchronizer)", this);
        }

        logger.Log($"[PlayerSpawnManager] Player {playerIdentity.netId} position after reposition: {playerIdentity.transform.position}", this);
    }

    private void OnClientConnectedSpawn(NetworkConnectionToClient conn) {
        if (spawnPoints == null || spawnPoints.Length == 0) {
            logger.Log($"[PlayerSpawnManager] No spawn points configured for connection {conn.connectionId}", this, Logging.LogType.Warning);
            return;
        }

        // Wait a bit for Mirror to spawn the player automatically
        StartCoroutine(RepositionAfterSpawn(conn));
    }

    private System.Collections.IEnumerator RepositionAfterSpawn(NetworkConnectionToClient conn) {
        // Minimal delay to ensure player is spawned
        yield return new WaitForFixedUpdate();

        if (conn.identity != null) {
            GameObject player = conn.identity.gameObject;
            logger.Log($"[PlayerSpawnManager] Connection {conn.connectionId} spawned. Player current position: {player.transform.position}, Rotation: {player.transform.rotation.eulerAngles}", this);

            int playerIndex = conn.connectionId;
            logger.Log($"[PlayerSpawnManager] New connection {conn.connectionId} connected, repositioning to spawn point...", this);
            Coroutine coroutine = StartCoroutine(RepositionPlayerWithDelay(conn.identity, playerIndex));
            activeCoroutines.Add(coroutine);
        } else {
            logger.Log($"[PlayerSpawnManager] Connection {conn.connectionId} has no identity after spawn", this, Logging.LogType.Warning);
        }
    }

    private Vector3 GetSpawnPositionByIndex(int index) {
        if (spawnPoints != null && spawnPoints.Length > 0) {
            int spawnIndex = index % spawnPoints.Length;
            Vector3 position = spawnPoints[spawnIndex].position;

            if (adjustToGroundHeight && groundReference != null) {
                position.y = groundReference.position.y + heightOffset;
                logger.Log($"[PlayerSpawnManager] Height adjusted to ground. Original Y: {spawnPoints[spawnIndex].position.y}, Adjusted Y: {position.y}", this);
            }

            return position;
        }

        logger.Log("[PlayerSpawnManager] No spawn points, using Vector3.zero", this, Logging.LogType.Warning);
        return Vector3.zero;
    }

    private Quaternion GetSpawnRotationByIndex(int index) {
        if (spawnPoints != null && spawnPoints.Length > 0) {
            int spawnIndex = index % spawnPoints.Length;
            return spawnPoints[spawnIndex].rotation;
        }

        return Quaternion.identity;
    }

    public override void OnStopServer() {
        NetworkServer.OnConnectedEvent -= OnClientConnectedSpawn;

        // Stop all active coroutines
        StopAllActiveCoroutines();
    }

    private void OnDestroy() {
        // Cleanup coroutines in case OnStopServer wasn't called
        StopAllActiveCoroutines();
    }

    private void StopAllActiveCoroutines() {
        if (repositionCoroutine != null) {
            StopCoroutine(repositionCoroutine);
            repositionCoroutine = null;
        }

        foreach (var coroutine in activeCoroutines) {
            if (coroutine != null) {
                StopCoroutine(coroutine);
            }
        }
        activeCoroutines.Clear();
    }
}
