using UnityEngine;
using System.Collections;
using Items;

/// <summary>
/// Bootstrap script that initializes the correct ItemsManager based on context.
/// - Dedicated Server: Uses DedicatedServerItemsManager (metadata only)
/// - Client/Host: Uses ItemsManager (metadata + sprites)
/// Place this in MPMenuScene to ensure items are loaded before gameplay.
/// </summary>
public class ItemsManagerBootstrap : MonoBehaviour {
    [Header("Manager References")]
    [SerializeField]
    [Tooltip("ItemsManager for clients (loads metadata + sprites)")]
    private ItemsManager clientItemsManager;
    
    [SerializeField]
    [Tooltip("DedicatedServerItemsManager for server (loads metadata only)")]
    private DedicatedServerItemsManager serverItemsManager;

    [Header("Debug")]
    [SerializeField]
    private bool enableDebugLogs = true;

    private void Start() {
        StartCoroutine(WaitForNetworkThenInitialize());
    }

    private IEnumerator WaitForNetworkThenInitialize() {
        // Wait for NetworkManager to be initialized (if in multiplayer)
        float timeout = 5f;
        float elapsed = 0f;
        
        while (Unity.Netcode.NetworkManager.Singleton == null && elapsed < timeout) {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (Unity.Netcode.NetworkManager.Singleton == null) {
            DebugLog("NetworkManager not found after timeout, assuming single-player mode");
        }
        
        yield return InitializeCorrectManager();
    }

    private IEnumerator InitializeCorrectManager() {
        // RUNTIME detection: Check if we are running as dedicated server
        bool isDedicatedServer = Unity.Netcode.NetworkManager.Singleton != null && 
                                  Unity.Netcode.NetworkManager.Singleton.IsListening &&
                                  Unity.Netcode.NetworkManager.Singleton.IsServer && 
                                  !Unity.Netcode.NetworkManager.Singleton.IsClient;
        
        // Fallback: Check application batch mode (dedicated server usually runs headless)
        if (!isDedicatedServer && Application.isBatchMode) {
            isDedicatedServer = true;
        }
        
        if (isDedicatedServer) {
            // Servidor dedicado: usar DedicatedServerItemsManager
            DebugLog("Running as DEDICATED SERVER - initializing DedicatedServerItemsManager");
            
            if (serverItemsManager != null) {
                // Desactivar el cliente manager si existe
                if (clientItemsManager != null) {
                    clientItemsManager.gameObject.SetActive(false);
                    DebugLog("Disabled clientItemsManager (not needed on server)");
                }
                
                yield return serverItemsManager.Initialize();
                
                if (serverItemsManager.IsInitialized) {
                    DebugLog($"✅ DedicatedServerItemsManager initialized with {serverItemsManager.TotalItemsLoaded} items");
                } else {
                    Debug.LogError("[ItemsManagerBootstrap] ❌ Failed to initialize DedicatedServerItemsManager!");
                }
            } else {
                Debug.LogError("[ItemsManagerBootstrap] ❌ DedicatedServerItemsManager not assigned!");
            }
        } else {
            // Cliente o Host: usar ItemsManager
            DebugLog("Running as CLIENT/HOST - initializing ItemsManager");
            
            if (clientItemsManager != null) {
                // Desactivar el servidor manager si existe
                if (serverItemsManager != null) {
                    serverItemsManager.gameObject.SetActive(false);
                    DebugLog("Disabled serverItemsManager (not needed on client)");
                }
                
                // ItemsManager se inicializa automáticamente en Start(), solo esperamos
                while (!clientItemsManager.IsInitialized) {
                    yield return new WaitForSeconds(0.1f);
                }
                
                DebugLog($"✅ ItemsManager initialized with {clientItemsManager.TotalItemsLoaded} items, {clientItemsManager.TotalSpritesLoaded} sprites");
            } else {
                Debug.LogError("[ItemsManagerBootstrap] ❌ ItemsManager not assigned!");
            }
        }
    }

    private void DebugLog(string message) {
        if (enableDebugLogs) {
            Debug.Log($"[ItemsManagerBootstrap] {message}");
        }
    }
}
