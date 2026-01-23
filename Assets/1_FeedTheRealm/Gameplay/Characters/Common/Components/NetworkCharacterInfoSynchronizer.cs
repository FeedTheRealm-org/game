using Mirror;
using UnityEngine;

/// <summary>
/// Synchronizes character sprite information across the network.
/// Local player sends their UserID to the server, which then syncs it to all clients.
/// Remote clients use the UserID to load the appropriate character sprites.
/// </summary>
public class NetworkCharacterInfoSynchronizer : NetworkBehaviour
{
    [SerializeField]
    private SpriteLoader spriteLoader;

    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private Logging.Logger logger;

    [SyncVar(hook = nameof(OnUserIdChanged))]
    private string syncedUserId = "";

    public override void OnStartClient()
    {
        logger?.Log(
            $"[NetworkCharacterInfoSynchronizer] OnStartClient - isLocalPlayer: {isLocalPlayer}, netId: {netId}",
            this
        );

        if (isLocalPlayer)
        {
            // Local player: Send UserID to server and start loading immediately
            string userId = session?.UserId;
            logger?.Log(
                $"[NetworkCharacterInfoSynchronizer] Local player UserID from session: '{userId}'",
                this
            );

            if (!string.IsNullOrEmpty(userId))
            {
                CmdSendUserId(userId);

                // Start loading sprites immediately for local player
                if (spriteLoader != null)
                {
                    spriteLoader.UserId = userId;
                    spriteLoader.StartLoadingSprites();
                }

                logger?.Log(
                    $"[NetworkCharacterInfoSynchronizer] Local player sent UserID: {userId}",
                    this
                );
            }
            else
            {
                logger?.Log(
                    "[NetworkCharacterInfoSynchronizer] Local player UserID is null or empty! Sprite loading will use default.",
                    this,
                    Logging.LogType.Error
                );

                // Still start loading with null UserId (will use default sprites)
                if (spriteLoader != null)
                {
                    spriteLoader.StartLoadingSprites();
                }
            }
        }
        else
        {
            // Remote player: Check if UserID is already synced
            if (!string.IsNullOrEmpty(syncedUserId))
            {
                LoadSpritesForRemotePlayer(syncedUserId);
            }
            else
            {
                // Wait for SyncVar to arrive
                StartCoroutine(CheckSyncedUserIdDelayed());
            }
        }
    }

    [Command]
    private void CmdSendUserId(string userId)
    {
        syncedUserId = userId;
        logger?.Log(
            $"[NetworkCharacterInfoSynchronizer] Server received UserID: '{userId}', netId: {netId}",
            this
        );
    }

    // SyncVar hook called on all clients (including local) when syncedUserId changes
    private void OnUserIdChanged(string oldValue, string newValue)
    {
        logger?.Log(
            $"[NetworkCharacterInfoSynchronizer] OnUserIdChanged: old='{oldValue}', new='{newValue}', isLocalPlayer={isLocalPlayer}",
            this
        );

        // Only load sprites for remote players (local player already loaded their own)
        if (!isLocalPlayer && !string.IsNullOrEmpty(newValue))
        {
            LoadSpritesForRemotePlayer(newValue);
        }
    }

    private void LoadSpritesForRemotePlayer(string userId)
    {
        if (spriteLoader != null)
        {
            spriteLoader.UserId = userId;
            spriteLoader.StartLoadingSprites();
            logger?.Log(
                $"[NetworkCharacterInfoSynchronizer] Remote player loading sprites for UserID: {userId}",
                this
            );
        }
    }

    private System.Collections.IEnumerator CheckSyncedUserIdDelayed()
    {
        // Wait for SyncVar sync with timeout
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout && string.IsNullOrEmpty(syncedUserId))
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (!string.IsNullOrEmpty(syncedUserId))
        {
            LoadSpritesForRemotePlayer(syncedUserId);
            logger?.Log(
                $"[NetworkCharacterInfoSynchronizer] Remote player found synced UserID after wait: {syncedUserId}",
                this
            );
        }
        else
        {
            logger?.Log(
                $"[NetworkCharacterInfoSynchronizer] Remote player timed out waiting for synced UserID after {timeout}s",
                this,
                Logging.LogType.Warning
            );
        }
    }
}
