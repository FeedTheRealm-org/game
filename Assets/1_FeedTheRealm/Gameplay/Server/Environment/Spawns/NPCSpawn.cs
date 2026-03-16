using FTR.Core.Common.Scopes;
using FTR.Gameplay.Common.Environment.Npcs;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;
using VContainer.Unity;

public class NPCSpawns : MonoBehaviour
{
    [Header("Spawn settings")]
    [SerializeField]
    private GameObject npcPrefab;

    [SerializeField]
    private string npcID;

    [SerializeField]
    private float radius = 5f;

    [SerializeField]
    private ObjectResolverContainer resolverContainer;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private NPCData npcData;
    private bool isInitialized = false;

    /// <summary>
    /// Configures this spawner with data from the world loader.
    /// npcData provides the id and name for NpcIdentity.
    /// </summary>
    public void Initialize(NPCSpawnerData spawnData, NPCData npcData, DialogData dialogData = null)
    {
        if (spawnData == null)
            throw new System.ArgumentNullException(
                nameof(spawnData),
                "NPCSpawnerData cannot be null when initializing NPCSpawns."
            );

        if (npcData == null)
            throw new System.ArgumentNullException(
                nameof(npcData),
                "NPCData cannot be null when initializing NPCSpawns."
            );

        this.npcID = spawnData.NpcId;
        this.npcData = npcData;
        this.radius = spawnData.Radius;

        if (!isInitialized)
        {
            isInitialized = true;
            TrySpawnNPC();
        }
    }

    private void TrySpawnNPC()
    {
        if (NetworkServer.active)
        {
            SpawnNPC();
            return;
        }

        logger.Log("[NPCSpawns] NetworkServer not active yet, waiting to spawn NPC.", this);
        StartCoroutine(SpawnWhenServerActive());
    }

    private System.Collections.IEnumerator SpawnWhenServerActive()
    {
        yield return new WaitUntil(() => NetworkServer.active);
        SpawnNPC();
    }

    private void SpawnNPC()
    {
        if (npcPrefab == null)
            throw new System.Exception("[NPCSpawns] NPC prefab not assigned!");

        var position = GetRandomPointInRadius();
        logger.Log($"[NPCSpawns] Spawning NPC '{npcID}' at {position}", this);

        GameObject npc = resolverContainer.Resolver.Instantiate(
            npcPrefab,
            position,
            Quaternion.identity
        );
        npc.name = $"NPC_{npcID}";

        var identity = npc.GetComponent<NpcIdentity>();
        if (identity != null)
            identity.Initialize(npcData);
        else
            Debug.LogWarning(
                $"[NPCSpawns] NpcIdentity component not found on prefab for NPC '{npcID}'."
            );

        NetworkServer.Spawn(npc);
    }

    private Vector3 GetRandomPointInRadius()
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        return transform.position
            + new Vector3(randomCircle.x, transform.position.y, randomCircle.y);
    }

#if DEBUG
    private System.Collections.IEnumerator Start()
    {
        yield return new WaitUntil(() => NetworkServer.active);
        logger.Log("[NPCSpawns] Resolver already set, spawning NPC immediately.", this);
        if (!isInitialized)
        {
            SpawnNPC();
            isInitialized = true;
        }
    }
#endif

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
