using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class NetworkCharacterInfoSynchronizer : NetworkBehaviour {
    [SerializeField] private SpriteLoader spriteLoader;
    [SerializeField] private Session.Session session;
    [SerializeField] private Logging.Logger logger;

    private NetworkVariable<FixedString128Bytes> syncedUserId = new NetworkVariable<FixedString128Bytes>(
        writePerm: NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn() {
        logger?.Log($"[NetworkCharacterInfoSynchronizer] OnNetworkSpawn - IsOwner: {IsOwner}, ClientId: {OwnerClientId}", this);

        if (IsOwner) {
            // Local player: Send UserID to server and start loading immediately
            string userId = session?.UserId;
            logger?.Log($"[NetworkCharacterInfoSynchronizer] Local player UserID from session: '{userId}'", this);

            if (!string.IsNullOrEmpty(userId)) {
                SendUserIdToServerRpc(userId);
                spriteLoader.UserId = userId;
                spriteLoader.StartLoadingSprites();
                logger?.Log($"[NetworkCharacterInfoSynchronizer] Local player {OwnerClientId} sent UserID: {userId}", this);
            } else {
                logger?.Log("[NetworkCharacterInfoSynchronizer] Local player UserID is null or empty! Sprite loading will use default.", this, Logging.LogType.Error);
                // Still start loading with null UserId, which will use session.UserId (also null)
                spriteLoader.StartLoadingSprites();
            }
        } else {
            // Remote player: Subscribe to syncedUserId changes
            syncedUserId.OnValueChanged += OnSyncedUserIdChanged;
            logger?.Log($"[NetworkCharacterInfoSynchronizer] Remote player {OwnerClientId} subscribed to UserID sync", this);

            // Check if UserID is already synced (for late-joining clients)
            StartCoroutine(CheckSyncedUserIdDelayed());
        }
    }

    public override void OnNetworkDespawn() {
        if (!IsOwner) {
            syncedUserId.OnValueChanged -= OnSyncedUserIdChanged;
        }
    }

    [ServerRpc]
    private void SendUserIdToServerRpc(string userId) {
        syncedUserId.Value = userId;
        logger?.Log($"[NetworkCharacterInfoSynchronizer] Server received UserID for client {OwnerClientId}: '{userId}', setting syncedUserId to '{syncedUserId.Value}'", this);
    }

    private void OnSyncedUserIdChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue) {
        logger?.Log($"[NetworkCharacterInfoSynchronizer] OnSyncedUserIdChanged for {OwnerClientId}: old='{oldValue}', new='{newValue}', IsOwner={IsOwner}", this);

        if (!IsOwner && !newValue.IsEmpty) {
            string userIdString = newValue.ToString();
            spriteLoader.UserId = userIdString;
            spriteLoader.StartLoadingSprites();
            logger?.Log($"[NetworkCharacterInfoSynchronizer] Remote player {OwnerClientId} received UserID: {userIdString}, starting sprite load", this);
        }
    }

    private System.Collections.IEnumerator CheckSyncedUserIdDelayed() {
        // Wait for NetworkVariable sync with timeout
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout && syncedUserId.Value.IsEmpty) {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (!syncedUserId.Value.IsEmpty) {
            string userIdString = syncedUserId.Value.ToString();
            spriteLoader.UserId = userIdString;
            spriteLoader.StartLoadingSprites();
            logger?.Log($"[NetworkCharacterInfoSynchronizer] Remote player {OwnerClientId} found synced UserID after wait: {userIdString}, starting sprite load", this);
        } else {
            logger?.Log($"[NetworkCharacterInfoSynchronizer] Remote player {OwnerClientId} timed out waiting for synced UserID after {timeout}s", this);
        }
    }
}
