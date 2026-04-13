using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Scopes;
using FTR.Core.Server.Config;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using VContainer;
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

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private ObjectResolverContainer resolverContainer;

    private ServerConfig config;

    private NPCData npcData;
    private bool isInitialized = false;
    private bool navMeshReady = false;

    /// <summary>
    /// Configures this spawner with data from the world loader.
    /// </summary>
    public void Initialize(NPCSpawnerData spawnData, NPCData npcData, DialogData dialogData = null)
    {
        if (spawnData == null)
            throw new System.ArgumentNullException(
                nameof(spawnData),
                "NPCSpawnerData cannot be null when initializing Spawns."
            );

        if (npcData == null)
            throw new System.ArgumentNullException(
                nameof(npcData),
                "NPCData cannot be null when initializing NPCSpawns."
            );

        config = resolverContainer.Resolver.Resolve<ServerConfig>();

        if (config == null)
            throw new System.ArgumentNullException(
                nameof(config),
                "ServerConfig cannot be null when initializing NPCSpawns."
            );

        this.npcID = !string.IsNullOrEmpty(npcData.id) ? npcData.id : spawnData.NpcId;
        this.npcData = npcData;
        this.radius = spawnData.Radius;

        if (!isInitialized)
        {
            BuildNavMesh(radius + 5f);
            StartCoroutine(SpawnWhenServerActive());
            isInitialized = true;
        }
    }

    private IEnumerator SpawnWhenServerActive()
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

        var characterId = !string.IsNullOrEmpty(npcData?.id) ? npcData.id : npcID;
        var stateStorage = npc.GetComponent<CharacterStateStorage>();
        if (stateStorage != null && !string.IsNullOrEmpty(characterId))
        {
            stateStorage.SetCharacterId(characterId);
        }
        else if (stateStorage == null)
            Debug.LogWarning(
                $"[NPCSpawns] CharacterStateStorage component not found on prefab for NPC '{npcID}'."
            );
        else
            Debug.LogWarning("[NPCSpawns] CharacterId is empty, NPC sprite sync may fail.", this);

        NetworkServer.Spawn(npc);
    }

    private Vector3 GetRandomPointInRadius()
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        return transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    /// <summary>
    /// Builds a NavMesh around the spawn area to ensure enemies can navigate properly.
    /// </summary>
    private void BuildNavMesh(float navMeshRadius)
    {
        Bounds bounds = new Bounds(transform.position, Vector3.one * navMeshRadius * 2);

        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(
            bounds,
            config.GroundLayer,
            NavMeshCollectGeometry.PhysicsColliders,
            0, // Walkable
            new List<NavMeshBuildMarkup>(),
            sources
        );

        var obstacleSources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(
            bounds,
            config.ObstacleLayer,
            NavMeshCollectGeometry.PhysicsColliders,
            1, // Not Walkable
            new List<NavMeshBuildMarkup>(),
            obstacleSources
        );

        sources.AddRange(obstacleSources);

        var navMeshData = new NavMeshData();

        var buildOp = NavMeshBuilder.UpdateNavMeshDataAsync(
            navMeshData,
            UnityEngine.AI.NavMesh.GetSettingsByID(0),
            sources,
            bounds
        );

        UnityEngine.AI.NavMesh.AddNavMeshData(navMeshData);

        StartCoroutine(WaitForNavMesh(buildOp));
    }

    /// <summary>
    /// Waits for the NavMesh to be built before allowing enemy spawns.
    /// </summary>
    IEnumerator WaitForNavMesh(AsyncOperation buildOp)
    {
        while (!buildOp.isDone)
            yield return null;

        navMeshReady = true;
    }

#if DEBUG
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => NetworkServer.active);

        var npcSpawnerData = new NPCSpawnerData(transform.position, radius, npcID);
        var npcData = new NPCData(
            npcID,
            $"NPC_{npcID}",
            "A friendly NPC.",
            null,
            new Dictionary<string, string>()
        );
        Initialize(npcSpawnerData, npcData);
    }
#endif

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);

        if (!Application.isPlaying || !navMeshReady)
            return;

        Gizmos.color = Color.blue;

        var triangulation = NavMesh.CalculateTriangulation();

        for (int i = 0; i < triangulation.indices.Length; i += 3)
        {
            Vector3 v0 = triangulation.vertices[triangulation.indices[i]];
            Vector3 v1 = triangulation.vertices[triangulation.indices[i + 1]];
            Vector3 v2 = triangulation.vertices[triangulation.indices[i + 2]];

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }
    }
}
