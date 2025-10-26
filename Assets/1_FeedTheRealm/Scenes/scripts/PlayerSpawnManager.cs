using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    
    [Header("Auto-detect Spawn Points")]
    [SerializeField] private bool autoDetectSpawnPoints = true;
    [SerializeField] private string spawnPointTag = "SpawnPoint";
    
    [Header("Ground Reference")]
    [SerializeField] private Transform groundReference;
    [SerializeField] private bool adjustToGroundHeight = true;
    [SerializeField] private float heightOffset = 0f; // Altura adicional sobre el suelo

    [Header("Network Settings")]
    [SerializeField] private int maxPlayers = 100;
    
    [SerializeField] private Logging.Logger logger;
    
    // Store coroutine references for cleanup
    private Coroutine repositionCoroutine;
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    private void Awake()
    {
        // Auto-detectar spawn points si está habilitado
        if (autoDetectSpawnPoints)
        {
            DetectSpawnPoints();
        }
    }

    private void DetectSpawnPoints()
    {
        List<Transform> detectedSpawnPoints = new List<Transform>();
        
        // Buscar hijos directos
        foreach (Transform child in transform)
        {
            detectedSpawnPoints.Add(child);
        }
        
        // Si también hay objetos con tag
        GameObject[] taggedSpawnPoints = GameObject.FindGameObjectsWithTag(spawnPointTag);
        foreach (GameObject spawnObj in taggedSpawnPoints)
        {
            if (!detectedSpawnPoints.Contains(spawnObj.transform))
            {
                detectedSpawnPoints.Add(spawnObj.transform);
            }
        }
        
        if (detectedSpawnPoints.Count > 0)
        {
            spawnPoints = detectedSpawnPoints.ToArray();
            logger.Log($"[PlayerSpawnManager] Detected {spawnPoints.Length} spawn points automatically", this);
        }
        else
        {
            logger.Log("[PlayerSpawnManager] No spawn points detected. Using default position.", this, Logging.LogType.Warning);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                logger.Log("[PlayerSpawnManager] No spawn points configured!", this, Logging.LogType.Error);
            }
            else
            {
                logger.Log($"[PlayerSpawnManager] Spawn Manager activated. Max players: {maxPlayers}, Spawn points: {spawnPoints.Length}", this);
                
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    logger.Log($"[PlayerSpawnManager] Spawn Point {i}: Position = {spawnPoints[i].position}, Rotation = {spawnPoints[i].rotation.eulerAngles}", this);
                }
            }
            
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedSpawn;
            
            // Configurar límites para MMO
            NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApprovalCallback;
            
            // Reposicionar jugadores existentes cuando se carga una nueva escena
            // Usar delay más grande para asegurar que todo esté inicializado
            repositionCoroutine = StartCoroutine(DelayedRepositionPlayers());
        }
    }
    
    private System.Collections.IEnumerator DelayedRepositionPlayers()
    {
        // Esperar más tiempo para que los jugadores estén completamente inicializados
        yield return new WaitForSeconds(0.2f);
        
        RepositionExistingPlayers();
        repositionCoroutine = null;
    }
    
    private void RepositionExistingPlayers()
    {
        logger.Log($"[PlayerSpawnManager] Repositioning existing players in scene... Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}", this);
        
        int playerIndex = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject != null)
            {
                logger.Log($"[PlayerSpawnManager] Found player {client.Key} with PlayerObject", this);
                Coroutine coroutine = StartCoroutine(RepositionPlayerWithDelay(client.Value.PlayerObject, playerIndex));
                activeCoroutines.Add(coroutine);
                playerIndex++;
            }
            else
            {
                logger.Log($"[PlayerSpawnManager] Client {client.Key} has no PlayerObject yet", this, Logging.LogType.Warning);
            }
        }
        
        logger.Log($"[PlayerSpawnManager] Initiated repositioning for {playerIndex} player(s)", this);
    }
    
    private System.Collections.IEnumerator RepositionPlayerWithDelay(NetworkObject playerObject, int index)
    {
        logger.Log($"[PlayerSpawnManager] Starting delayed reposition for player {playerObject.OwnerClientId}, waiting for NetworkObject to be ready...", this);
        
        yield return new WaitForFixedUpdate();
        
        RepositionPlayer(playerObject, index);
        
        // Remove from active coroutines list when complete
        // Note: Can't reference 'this' coroutine directly, cleanup happens in OnNetworkDespawn/OnDestroy
    }
    
    private void RepositionPlayer(NetworkObject playerObject, int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            logger.Log("[PlayerSpawnManager] No spawn points to reposition", this, Logging.LogType.Warning);
            return;
        }
        
        int spawnIndex = index % spawnPoints.Length;
        Vector3 newPosition = GetSpawnPositionByIndex(index);
        Quaternion newRotation = GetSpawnRotationByIndex(index);
        
        logger.Log($"[PlayerSpawnManager] Repositioning player {playerObject.OwnerClientId} to spawn {spawnIndex} at {newPosition}, current position: {playerObject.transform.position}", this);
        
        var synchronizer = playerObject.GetComponent<NetworkMovementSynchronizer>();
        if (synchronizer != null)
        {
            synchronizer.Teleport(newPosition);
            logger.Log($"[PlayerSpawnManager] Player {playerObject.OwnerClientId} teleported to spawn {spawnIndex} at {newPosition} via NetworkMovementSynchronizer", this);
        }
        else
        {
            playerObject.transform.SetPositionAndRotation(newPosition, newRotation);
            logger.Log($"[PlayerSpawnManager] Player {playerObject.OwnerClientId} repositioned to spawn {spawnIndex} at {newPosition} (no NetworkMovementSynchronizer found, using transform)", this);
        }
        
        logger.Log($"[PlayerSpawnManager] Player {playerObject.OwnerClientId} position after reposition: {playerObject.transform.position}", this);
    }

    private void ConnectionApprovalCallback(
        NetworkManager.ConnectionApprovalRequest request, 
        NetworkManager.ConnectionApprovalResponse response)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count < maxPlayers)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
            
            int nextClientIndex = NetworkManager.Singleton.ConnectedClients.Count;
            
            Vector3 spawnPos = GetSpawnPositionByIndex(nextClientIndex);
            Quaternion spawnRot = GetSpawnRotationByIndex(nextClientIndex);
            
            response.Position = spawnPos;
            response.Rotation = spawnRot;

            logger.Log($"[PlayerSpawnManager] Client approved. Index: {nextClientIndex}, Spawn index: {nextClientIndex % (spawnPoints?.Length ?? 1)}, Assigned position: {spawnPos}, Rotation: {spawnRot.eulerAngles}", this);
        }
        else
        {
            response.Approved = false;
            response.Reason = "Server full";
            logger.Log($"[PlayerSpawnManager] Connection rejected: server full ({NetworkManager.Singleton.ConnectedClients.Count}/{maxPlayers})", this, Logging.LogType.Warning);
        }
    }

    private void OnClientConnectedSpawn(ulong clientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            logger.Log($"[PlayerSpawnManager] No spawn points configured for client {clientId}", this, Logging.LogType.Warning);
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            logger.Log($"[PlayerSpawnManager] Could not find client {clientId}", this, Logging.LogType.Warning);
            return;
        }

        if (client.PlayerObject == null)
        {
            logger.Log($"[PlayerSpawnManager] PlayerObject not ready for client {clientId}", this, Logging.LogType.Warning);
            return;
        }

        GameObject player = client.PlayerObject.gameObject;
        logger.Log($"[PlayerSpawnManager] Client {clientId} connected. Player current position: {player.transform.position}, Rotation: {player.transform.rotation.eulerAngles}", this);
        
        int playerIndex = (int)clientId;
        logger.Log($"[PlayerSpawnManager] New client {clientId} connected, repositioning to spawn point...", this);
        Coroutine coroutine = StartCoroutine(RepositionPlayerWithDelay(client.PlayerObject, playerIndex));
        activeCoroutines.Add(coroutine);
    }

    private Vector3 GetSpawnPositionByIndex(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = index % spawnPoints.Length;
            Vector3 position = spawnPoints[spawnIndex].position;
            
            if (adjustToGroundHeight && groundReference != null)
            {
                position.y = groundReference.position.y + heightOffset;
                logger.Log($"[PlayerSpawnManager] Height adjusted to ground. Original Y: {spawnPoints[spawnIndex].position.y}, Adjusted Y: {position.y}", this);
            }
            
            return position;
        }

        logger.Log("[PlayerSpawnManager] No spawn points, using Vector3.zero", this, Logging.LogType.Warning);
        return Vector3.zero;
    }

    private Quaternion GetSpawnRotationByIndex(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = index % spawnPoints.Length;
            return spawnPoints[spawnIndex].rotation;
        }
        
        return Quaternion.identity;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedSpawn;
            NetworkManager.Singleton.ConnectionApprovalCallback -= ConnectionApprovalCallback;
        }
        
        // Stop all active coroutines
        StopAllActiveCoroutines();
    }
    
    public override void OnDestroy()
    {
        // Cleanup coroutines in case OnNetworkDespawn wasn't called
        StopAllActiveCoroutines();
        base.OnDestroy();
    }
    
    private void StopAllActiveCoroutines()
    {
        if (repositionCoroutine != null)
        {
            StopCoroutine(repositionCoroutine);
            repositionCoroutine = null;
        }
        
        foreach (var coroutine in activeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        activeCoroutines.Clear();
    }
}