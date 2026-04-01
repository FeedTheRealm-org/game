using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;

/// <summary>
/// Client-side component that monitors proximity to the currently active NPC.
/// When the player leaves the interaction radius, dispatches CancelInteract to the server.
/// Only active while CharacterInteractingState or CharacterQuestState is current.
/// </summary>
public class ProximityWatcher : MonoBehaviour
{
    /*[SerializeField]
    private float proximityRadius = 3.5f;

    private NetworkAdapter networkAdapter;
    private CharacterStateStorage stateStorage;
    private bool _watching;

    public void Initialize(NetworkAdapter networkAdapter, CharacterStateStorage stateStorage)
    {
        this.networkAdapter = networkAdapter;
        this.stateStorage = stateStorage;
        enabled = false; // inactive until StartWatching is called
    }

    /// <summary>
    /// Begin monitoring proximity to the given NPC id.
    /// Called by CharacterInteractingState.Enter() and CharacterQuestState.Enter().
    /// </summary>
    public void StartWatching()
    {
        _watching = true;
        enabled = true;
    }

    /// <summary>
    /// Stop monitoring. Called on Exit() of interacting/quest states,
    /// or when CancelInteract has already been dispatched.
    /// </summary>
    public void StopWatching()
    {
        _watching = false;
        enabled = false;
    }

    private void Update()
    {
        if (!_watching || !stateStorage.IsInteracting)
        {
            StopWatching();
            return;
        }

        var npcId = stateStorage.CurrentNpcId;
        if (string.IsNullOrEmpty(npcId))
            return;

        // Find the NPC in scene by NpcIdentity
        var allIdentities = FindObjectsByType<NpcIdentity>(FindObjectsSortMode.None);
        foreach (var identity in allIdentities)
        {
            if (identity.NpcId != npcId)
                continue;

            float dist = Vector3.Distance(transform.position, identity.transform.position);
            if (dist > proximityRadius)
            {
                Debug.Log($"[ProximityWatcher] Player left range of NPC '{npcId}'. Cancelling.");
                StopWatching();
                networkAdapter.DispatchAction(
                    new ActionCommandDTO { Type = ActionType.CancelInteract }
                );
            }
            return;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
    }
#endif*/
}
