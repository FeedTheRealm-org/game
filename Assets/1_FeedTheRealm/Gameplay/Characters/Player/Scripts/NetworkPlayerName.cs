using UnityEngine;
using Mirror;
using TMPro;

/// <summary>
/// Synchronizes and displays the player name above each networked player.
/// - SyncVar to replicate the name to all clients.
/// - Local player sends their name (from Session) to the server.
/// - Updates a world-space TextMeshPro (via PlayerNameBillboard).
/// </summary>
public class NetworkPlayerName : NetworkBehaviour {
    [Header("References")]
    [SerializeField] private Session.Session session;
    [SerializeField] private PlayerNameBillboard nameBillboard;
    [SerializeField] private Logging.Logger logger;

    [Header("Name Settings")]
    [SyncVar(hook = nameof(OnNameChanged))]
    private string playerName = "";

    public override void OnStartAuthority() {
        base.OnStartAuthority();

        if (nameBillboard == null) {
            nameBillboard = GetComponentInChildren<PlayerNameBillboard>();
        }

        // Only the local player sends their name to the server
        if (!isLocalPlayer) return;

        if (session == null) {
            logger?.Log("[NetworkPlayerName] Session reference is not assigned on local player.", this, Logging.LogType.Warning);
            return;
        }

        var name = string.IsNullOrEmpty(session.CharacterName) ? $"Player {netId}" : session.CharacterName;
        CmdSetPlayerName(name);
    }

    private void Start() {
        if (nameBillboard == null) {
            nameBillboard = GetComponentInChildren<PlayerNameBillboard>();
        }

        if (!string.IsNullOrEmpty(playerName)) {
            ApplyName(playerName);
        }
    }

    [Command]
    private void CmdSetPlayerName(string name) {
        // Optionally clamp length / sanitize here
        if (string.IsNullOrWhiteSpace(name)) {
            name = $"Player {netId}";
        }

        playerName = name;
        logger?.Log($"[NetworkPlayerName] Server set name for netId={netId}: {playerName}", this);
    }

    private void OnNameChanged(string oldName, string newName) {
        ApplyName(newName);
    }

    private void ApplyName(string name) {
        if (nameBillboard == null) {
            nameBillboard = GetComponentInChildren<PlayerNameBillboard>();
        }

        if (nameBillboard != null) {
            nameBillboard.SetName(name);
        } else {
            logger?.Log($"[NetworkPlayerName] PlayerNameBillboard not found for netId={netId}", this, Logging.LogType.Warning);
        }
    }
}
