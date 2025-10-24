using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    
    [Header("Network Settings")] 
    [SerializeField] private int maxPlayers = 100;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log($"Spawn Manager activado. Máx jugadores: {maxPlayers}");
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedSpawn;
            
            // Configurar límites para MMO
            NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApprovalCallback;
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
            response.Position = GetSpawnPosition();
            response.Rotation = Quaternion.identity;
        }
        else
        {
            response.Approved = false;
            response.Reason = "Servidor lleno";
        }
    }

    private void OnClientConnectedSpawn(ulong clientId)
    {
        if (spawnPoints.Length > 0)
        {
            int spawnIndex = (int)(clientId % (ulong)spawnPoints.Length);
            GameObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
            
            var synchronizer = player.GetComponent<NetworkMovementSynchronizer>();
            if (synchronizer != null)
            {
                synchronizer.Teleport(spawnPoints[spawnIndex].position);
            }
            else
            {
                player.transform.SetPositionAndRotation(
                    spawnPoints[spawnIndex].position,
                    spawnPoints[spawnIndex].rotation
                );
            }
            
            Debug.Log($"Jugador {clientId} spawn en posición {spawnIndex}");
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints.Length > 0)
        {
            int spawnIndex = NetworkManager.Singleton.ConnectedClients.Count % spawnPoints.Length;
            return spawnPoints[spawnIndex].position;
        }
        return Vector3.zero;
    }

    // Resto del código igual...
}