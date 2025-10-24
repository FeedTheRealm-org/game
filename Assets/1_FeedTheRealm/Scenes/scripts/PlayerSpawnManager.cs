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
            logger.Log($"[PlayerSpawnManager] Detected {spawnPoints.Length} spawn points automatically", this, Logging.LogType.Info);
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
            // Validar spawn points
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                logger.Log("[PlayerSpawnManager] No spawn points configured!", this, Logging.LogType.Error);
            }
            else
            {
                logger.Log($"[PlayerSpawnManager] Spawn Manager activated. Max players: {maxPlayers}, Spawn points: {spawnPoints.Length}", this, Logging.LogType.Info);
                
                // Debug: Imprimir posiciones de spawn points
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    logger.Log($"[PlayerSpawnManager] Spawn Point {i}: Position = {spawnPoints[i].position}, Rotation = {spawnPoints[i].rotation.eulerAngles}", this, Logging.LogType.Info);
                }
            }
            
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedSpawn;
            
            // Configurar límites para MMO
            NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApprovalCallback;
            
            // IMPORTANTE: Reposicionar jugadores existentes cuando se carga una nueva escena
            RepositionExistingPlayers();
        }
    }
    
    private void RepositionExistingPlayers()
    {
        logger.Log("[PlayerSpawnManager] Repositioning existing players in scene...", this, Logging.LogType.Info);
        
        int playerIndex = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject != null)
            {
                RepositionPlayer(client.Value.PlayerObject, playerIndex);
                playerIndex++;
            }
        }
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
        
        // Intentar usar NetworkMovementSynchronizer si existe
        var synchronizer = playerObject.GetComponent<NetworkMovementSynchronizer>();
        if (synchronizer != null)
        {
            synchronizer.Teleport(newPosition);
            logger.Log($"[PlayerSpawnManager] Player {playerObject.OwnerClientId} teleported to spawn {spawnIndex} at {newPosition} via NetworkMovementSynchronizer", this, Logging.LogType.Info);
        }
        else
        {
            // Fallback: posicionar directamente
            playerObject.transform.SetPositionAndRotation(newPosition, newRotation);
            logger.Log($"[PlayerSpawnManager] Player {playerObject.OwnerClientId} repositioned to spawn {spawnIndex} at {newPosition}", this, Logging.LogType.Info);
        }
    }

    private void ConnectionApprovalCallback(
        NetworkManager.ConnectionApprovalRequest request, 
        NetworkManager.ConnectionApprovalResponse response)
    {
        // Aprobar conexión si no excedemos el máximo
        if (NetworkManager.Singleton.ConnectedClients.Count < maxPlayers)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
            
            // Calcular el spawn index basado en el número actual de clientes
            int nextClientIndex = NetworkManager.Singleton.ConnectedClients.Count;
            
            // Asignar posición de spawn
            Vector3 spawnPos = GetSpawnPositionByIndex(nextClientIndex);
            Quaternion spawnRot = GetSpawnRotationByIndex(nextClientIndex);
            
            response.Position = spawnPos;
            response.Rotation = spawnRot;

            logger.Log($"[PlayerSpawnManager] Client approved. Index: {nextClientIndex}, Spawn index: {nextClientIndex % (spawnPoints?.Length ?? 1)}, Assigned position: {spawnPos}, Rotation: {spawnRot.eulerAngles}", this, Logging.LogType.Info);
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
        logger.Log($"[PlayerSpawnManager] Client {clientId} connected. Player current position: {player.transform.position}, Rotation: {player.transform.rotation.eulerAngles}", this, Logging.LogType.Info);
    }

    private Vector3 GetSpawnPositionByIndex(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = index % spawnPoints.Length;
            Vector3 position = spawnPoints[spawnIndex].position;
            
            // Ajustar altura al suelo si está habilitado
            if (adjustToGroundHeight && groundReference != null)
            {
                position.y = groundReference.position.y + heightOffset;
                logger.Log($"[PlayerSpawnManager] Height adjusted to ground. Original Y: {spawnPoints[spawnIndex].position.y}, Adjusted Y: {position.y}", this, Logging.LogType.Info);
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
    }
}