using System;
using Mirror;
using UnityEngine;

/// <summary>
/// Tracks the gold amount for a player in a server-authoritative way.
/// Gold is modified only on the server and synchronized to clients via SyncVar.
/// HUD and other systems can subscribe to OnGoldChanged to update UI.
/// </summary>
public class PlayerGold : NetworkBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    [SyncVar(hook = nameof(OnGoldSyncVarChanged))]
    private int gold;

    /// <summary>
    /// Current gold amount for this player.
    /// </summary>
    public int Gold => gold;

    /// <summary>
    /// Event invoked on clients (and server) whenever gold changes.
    /// Provides the new gold value.
    /// </summary>
    public event Action<int> OnGoldChanged;

    [Server]
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int oldGold = gold;
        gold += amount;

        logger?.Log($"[PlayerGold] Added {amount} gold (from {oldGold} to {gold})", this);
    }

    private void OnGoldSyncVarChanged(int oldValue, int newValue)
    {
        logger?.Log($"[PlayerGold] Gold changed from {oldValue} to {newValue}", this);
        OnGoldChanged?.Invoke(newValue);
    }
}
