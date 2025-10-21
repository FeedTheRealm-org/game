using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    
    [Header("Debug")]
    [SerializeField] private bool showSpawnGizmos = true;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log($"Spawn Manager activado. {spawnPoints.Length} puntos de spawn configurados.");
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedSpawn;
        }
    }

    private void OnClientConnectedSpawn(ulong clientId)
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No hay spawn points configurados!");
            return;
        }

        try
        {
            int spawnIndex = (int)(clientId % (ulong)spawnPoints.Length);
            GameObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
            
            // Resetear rotación también
            player.transform.SetPositionAndRotation(
                spawnPoints[spawnIndex].position,
                spawnPoints[spawnIndex].rotation
            );
            
            Debug.Log($"Jugador {clientId} spawn en posición {spawnIndex}: {spawnPoints[spawnIndex].position}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error en spawn del jugador {clientId}: {e.Message}");
        }
    }

    // Para visualizar los spawn points en el Editor
    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos || spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(spawnPoints[i].position + Vector3.up * 0.5f, new Vector3(0.5f, 1f, 0.5f));
                Gizmos.DrawRay(spawnPoints[i].position, spawnPoints[i].forward * 1f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(spawnPoints[i].position + Vector3.up * 1.5f, $"Spawn {i}");
                #endif
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedSpawn;
        }
    }
}