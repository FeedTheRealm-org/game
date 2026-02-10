using System.Collections;
using FTR.Core.Client.Dialogue;
using Mirror;
using UnityEngine;

public class NPCSpawns : MonoBehaviour
{
    [Header("NPC settings")]
    [SerializeField]
    private GameObject npcPrefab;

    [SerializeField]
    private int maxNPCs = 1;

    [Header("Spawn points settings")]
    [SerializeField]
    private Transform spawnPointContainer;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private Models.DialogData dialogData;
    private int currentNPCs;
    private Vector3 spawnCenter;
    private float spawnRadius = 1f;

    private void Start()
    {
        // initialize spawn center and radius
        spawnCenter = transform.position;
        SphereCollider startSphere = GetComponent<SphereCollider>();
        if (startSphere != null)
            spawnRadius = startSphere.radius;

        SpawnAllNPCs();
    }

    private void SpawnAllNPCs()
    {
        for (int i = 0; i < maxNPCs; i++)
        {
            Vector3 pos = GetRandomSpawnPosition();
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            SpawnNPC(pos, rot);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = spawnCenter;
        float radius = spawnRadius;

        if (radius <= 0f)
        {
            radius = 1f;
        }

        Vector2 rand2 = Random.insideUnitCircle * radius;
        Vector3 pos = new Vector3(center.x + rand2.x, 0.05f, center.z + rand2.y);
        return pos;
    }

    private void SpawnNPC(Vector3 position, Quaternion rotation)
    {
        if (currentNPCs >= maxNPCs)
            return;

        if (npcPrefab == null)
        {
            logger.Log("NPC prefab is not assigned!", this, Logging.LogType.Error);
            return;
        }

        logger.Log($"[NPCSpawns] Spawning NPC at {position}", this);
        GameObject npc = Instantiate(npcPrefab, position, rotation);

        npc.GetComponent<DialogManagerComponent>()
            ?.SetDialogs(dialogData != null ? transformDialogDataToNpcMessages(dialogData) : null);

        currentNPCs++;
        logger.Log($"[NPCSpawns] NPC spawned at {position}. Total NPCs: {currentNPCs}", this);
    }

    private NpcMessageData[] transformDialogDataToNpcMessages(Models.DialogData dialogData)
    {
        NpcMessageData[] npcMessages = new NpcMessageData[dialogData.messages.Count];

        for (int i = 0; i < dialogData.messages.Count; i++)
        {
            var msg = dialogData.messages[i];
            npcMessages[i] = new NpcMessageData(msg.Content, null);
        }

        return npcMessages;
    }

    /// <summary>
    /// Configures this spawn instance with data from NPCSpawnerData.
    /// Must be called after instantiation for dynamically placed spawns.
    /// </summary>
    public void ConfigureFromSpawnData(
        Models.NPCSpawnerData spawnData,
        Models.DialogData dialogData
    )
    {
        if (spawnData == null)
        {
            logger?.Log(
                "[NPCSpawns] ConfigureFromSpawnData called with null data!",
                this,
                Logging.LogType.Error
            );
            return;
        }

        transform.position = spawnData.Position;
        spawnCenter = transform.position;
        this.dialogData = dialogData;

        logger?.Log($"[NPCSpawns] Configuring {dialogData}", this);

        logger?.Log(
            $"[NPCSpawns] Configured spawn: position={spawnData.Position}, radius={spawnData.Radius}, maxNPCs={maxNPCs}",
            this
        );
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix; // Needed to translate to scene pos
        Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
