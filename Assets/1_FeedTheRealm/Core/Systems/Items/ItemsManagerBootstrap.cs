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

    private void Awake() {
        // Make this GameObject (and all children) persist across scenes
        DontDestroyOnLoad(gameObject);
        DebugLog("ItemsManagerBootstrap marked as DontDestroyOnLoad");
    }

    private void Start() {
        StartCoroutine(InitializeCorrectManager());
    }

    private IEnumerator InitializeCorrectManager() {
        // Detection logic for choosing the correct manager:
        // - Dedicated Server Build (headless): DedicatedServerItemsManager (no sprites)
        // - Client/Host (with UI): ItemsManager (with sprites)
        
        // The way to detect a dedicated server is batch mode
        // #if UNITY_SERVER is a project-wide define and is active even when playing as client in editor!
        
        bool isDedicatedServer = Application.isBatchMode;
        
        if (isDedicatedServer) {
            DebugLog("Detected batch mode (headless) - using DedicatedServerItemsManager");
        } else {
            // NOT batch mode = needs UI = needs sprites
            // This covers: Editor as Client, Editor as Host, Client builds, Host builds
            DebugLog("Detected interactive mode (needs UI) - using ItemsManager");
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
