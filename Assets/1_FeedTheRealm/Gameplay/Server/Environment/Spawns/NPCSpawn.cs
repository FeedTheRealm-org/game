using FTR.Core.Common.Dialogue;
using FTR.Core.Common.Scopes;
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
    private DialogData dialogData;

    [SerializeField]
    private float radius = 5f;

    [SerializeField]
    private ObjectResolverContainer resolverContainer;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private bool isInitialized = false;

    /// <summary>
    /// Initialize Configures this spawn instance with data from NPCSpawnerData.
    /// Must be called after instantiation for dynamically placed spawns.
    /// </summary>
    public void Initialize(NPCSpawnerData spawnData, DialogData dialogData)
    {
        if (spawnData == null)
            throw new System.ArgumentNullException(
                nameof(spawnData),
                "NPCSpawnerData cannot be null when initializing NPCSpawns."
            );

        this.npcID = spawnData.NpcId;
        this.dialogData = dialogData;
        this.radius = spawnData.Radius;

        isInitialized = true;
        SpawnNPC();
    }

    private void SpawnNPC()
    {
        if (npcPrefab == null)
            throw new System.Exception("NPC prefab not assigned on NPCSpawns!");

        var position = GetRandomPointInRadius();
        logger.Log($"[NPCSpawns] Spawning NPC at {position}", this);
        GameObject npc = resolverContainer.Resolver.Instantiate(
            npcPrefab,
            position,
            Quaternion.identity
        );
        npc.name = $"NPC_{npcID}";
        NetworkServer.Spawn(npc);

        var msgs = dialogData != null ? TransformDialogDataToNpcMessages(dialogData) : null;
        npc.GetComponent<DialogManagerComponent>()?.SetDialogs(msgs);
    }

    private NpcMessageData[] TransformDialogDataToNpcMessages(DialogData dialogData)
    {
        NpcMessageData[] npcMessages = new NpcMessageData[dialogData.messages.Count];

        for (int i = 0; i < dialogData.messages.Count; i++)
        {
            var msg = dialogData.messages[i];
            npcMessages[i] = new NpcMessageData(msg.Content, null);
        }

        return npcMessages;
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
        yield return new WaitForSeconds(2f); // Give time for mirror to init Network
        logger.Log("[NPCSpawns] Resolver already set, spawning NPC immediately.", this);
        SpawnNPC();
    }
#endif

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
