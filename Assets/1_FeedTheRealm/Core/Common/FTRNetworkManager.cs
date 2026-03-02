using System;
using FTR.Core.Common.Config;
using FTR.Core.Common.EventChannels;
// using Core.Systems.Worlds;
// using Core.Systems.Worlds.Loader;
using kcp2k;
using Mirror;
// using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer.Unity;

/*
    Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
    API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public class FTRNetworkManager : NetworkManager
{
    // Overrides the base singleton so we don't
    // have to cast to this type everywhere.
    public static new FTRNetworkManager singleton => (FTRNetworkManager)NetworkManager.singleton;

    // [SerializeField]
    // private WorldLoaderController worldLoader; // TODO: Client-Server is coupled by this controller

    [Header("--- Custom Fields ---")]
    [SerializeField]
    private LifetimeScope containerScope;

    [SerializeField]
    private InitiatePlayerEvent initiatePlayerEvent;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private Config config;

    /// <summary>
    /// Runs on both Server and Client
    /// Networking is NOT initialized when this fires
    /// </summary>
    public override void Awake()
    {
        base.Awake();
    }

    #region Unity Callbacks

    public override void OnValidate()
    {
        base.OnValidate();
    }

    /// <summary>
    /// Runs on both Server and Client
    /// Networking is NOT initialized when this fires
    /// </summary>
    public override void Start()
    {
        logger.Log("[NetworkManager] Starting NetworkManager...", this);
        base.Start();
        // Mirror does not recognize build modes, so we have to manually start it here
        // by either starting a server or a client based on the builds Scripts Defines
        // (you can see these symbols in the proper build profiles).
        if (config.RuntimeRole == RuntimeRole.Server)
        {
            KcpTransport kcp = Transport.active as KcpTransport;
            if (kcp != null)
                kcp.Port = config.Port;
            StartServer();
        }
        else if (config.RuntimeRole == RuntimeRole.Client)
        {
            StartClient();
        }
    }

    /// <summary>
    /// Runs on both Server and Client
    /// </summary>
    public override void LateUpdate()
    {
        base.LateUpdate();
    }

    /// <summary>
    /// Runs on both Server and Client
    /// </summary>
    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion

    #region Start & Stop

    /// <summary>
    /// Set the frame rate for a headless server.
    /// <para>Override if you wish to disable the behavior or set your own tick rate.</para>
    /// </summary>
    public override void ConfigureHeadlessFrameRate()
    {
        base.ConfigureHeadlessFrameRate();
    }

    /// <summary>
    /// called when quitting the application by closing the window / pressing stop in the editor
    /// </summary>
    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// This causes the server to switch scenes and sets the networkSceneName.
    /// <para>Clients that connect to this server will automatically switch to this scene. This is called automatically if onlineScene or offlineScene are set, but it can be called from user code to switch scenes again while the game is in progress. This automatically sets clients to be not-ready. The clients must call NetworkClient.Ready() again to participate in the new scene.</para>
    /// </summary>
    /// <param name="newSceneName"></param>
    public override void ServerChangeScene(string newSceneName)
    {
        base.ServerChangeScene(newSceneName);
    }

    /// <summary>
    /// Called from ServerChangeScene immediately before SceneManager.LoadSceneAsync is executed
    /// <para>This allows server to do work / cleanup / prep before the scene changes.</para>
    /// </summary>
    /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
    public override void OnServerChangeScene(string newSceneName) { }

    /// <summary>
    /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
    /// </summary>
    /// <param name="sceneName">The name of the new scene.</param>
    public override void OnServerSceneChanged(string sceneName) { }

    /// <summary>
    /// Called from ClientChangeScene immediately before SceneManager.LoadSceneAsync is executed
    /// <para>This allows client to do work / cleanup / prep before the scene changes.</para>
    /// </summary>
    /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
    /// <param name="sceneOperation">Scene operation that's about to happen</param>
    /// <param name="customHandling">true to indicate that scene loading will be handled through overrides</param>
    public override void OnClientChangeScene(
        string newSceneName,
        SceneOperation sceneOperation,
        bool customHandling
    ) { }

    /// <summary>
    /// Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
    /// <para>Scene changes can cause player objects to be destroyed. The default implementation of OnClientSceneChanged in the NetworkManager is to add a player object for the connection if no player object exists.</para>
    /// </summary>
    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
    }

    #endregion

    #region Server System Callbacks

    /// <summary>
    /// Called on the server when a new client connects.
    /// <para>Unity calls this on the Server when a Client connects to the Server. Use an override to tell the NetworkManager what to do when a client connects to the server.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        logger.Log(
            $"[NetworkManager] OnServerConnect called for connection {conn.connectionId}",
            this
        );
    }

    /// <summary>
    /// Called on the server when a client is ready.
    /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);
        logger.Log(
            $"[NetworkManager] OnServerReady called for connection {conn.connectionId}",
            this
        );
    }

    /// <summary>
    /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
    /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("OnServerAddPlayer START");
        GameObject player = AddPlayer();
        NetworkServer.AddPlayerForConnection(conn, player);
        logger.Log(
            $"[NetworkManager] OnServerAddPlayer called for connection {conn.connectionId}",
            this
        );
    }

    public override Transform GetStartPosition()
    {
        // TODO: Dont use find objects by type here and dont make the NetworkManager depend on Gameplay
        // Find all PlayerSpawnPoint instances created by loaders
        // PlayerSpawnPoint[] spawnPoints = FindObjectsByType<PlayerSpawnPoint>(
        //     FindObjectsSortMode.None
        // );

        // // Use PlayerSpawnPoint if available
        // if (spawnPoints != null && spawnPoints.Length > 0)
        // {
        //     int connectionCount = NetworkServer.connections.Count;
        //     int spawnIndex = (connectionCount - 1) % spawnPoints.Length;

        //     Transform spawnPoint = spawnPoints[spawnIndex].transform;
        //     logger.Lo, thisg(
        //         $"[NetworkManager] Using WorldData spawn point {spawnIndex}: {spawnPoint.position}"
        //     );
        //     return spawnPoint;
        // }

        // Fallback to default spawn (uses startPositions list or NetworkManager position)
        logger.Log(
            "[NetworkManager] No WorldData spawn points, using default spawn",
            this,
            Logging.LogType.Warning
        );
        return base.GetStartPosition();
    }

    /// <summary>
    /// Called on the server when a client disconnects.
    /// <para>This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
    }

    /// <summary>
    /// Called on server when transport raises an error.
    /// <para>NetworkConnection may be null.</para>
    /// </summary>
    /// <param name="conn">Connection of the client...may be null</param>
    /// <param name="transportError">TransportError enum</param>
    /// <param name="message">String message of the error.</param>
    public override void OnServerError(
        NetworkConnectionToClient conn,
        TransportError transportError,
        string message
    ) { }

    /// <summary>
    /// Called on server when transport raises an exception.
    /// <para>NetworkConnection may be null.</para>
    /// </summary>
    /// <param name="conn">Connection of the client...may be null</param>
    /// <param name="exception">Exception thrown from the Transport.</param>
    public override void OnServerTransportException(
        NetworkConnectionToClient conn,
        Exception exception
    ) { }

    #endregion

    #region Client System Callbacks

    /// <summary>
    /// Called on the client when connected to a server.
    /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
    /// </summary>
    public override void OnClientConnect()
    {
        Debug.Log("CLIENT CONNECTED");
        base.OnClientConnect();

        // Start coroutine to wait for local player to spawn and be ready
        StartCoroutine(WaitForLocalPlayerAndInitialize());
    }

    private System.Collections.IEnumerator WaitForLocalPlayerAndInitialize()
    {
        Debug.Log("Waiting for local player to spawn...");

        // Wait until the local player is spawned
        while (NetworkClient.localPlayer == null)
        {
            yield return null;
        }

        Debug.Log("Local player spawned, waiting for GameObject to be fully enabled...");

        // Wait one more frame to ensure OnEnable has been called on all components
        yield return null;

        Debug.Log("START Injecting local player gameobject to container");
        containerScope.Container.InjectGameObject(NetworkClient.localPlayer.gameObject);
        Debug.Log("DONE Injected local player gameobject to container!");

        // Now raise the event - CharacterInitializer should be subscribed by now
        initiatePlayerEvent.Raise();
        Debug.Log("InitiatePlayerEvent raised");
    }

    /// <summary>
    /// Called on clients when disconnected from a server.
    /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
    /// </summary>
    public override void OnClientDisconnect() { }

    /// <summary>
    /// Called on clients when a servers tells the client it is no longer ready.
    /// <para>This is commonly used when switching scenes.</para>
    /// </summary>
    public override void OnClientNotReady() { }

    /// <summary>
    /// Called on client when transport raises an error.</summary>
    /// </summary>
    /// <param name="transportError">TransportError enum.</param>
    /// <param name="message">String message of the error.</param>
    public override void OnClientError(TransportError transportError, string message) { }

    /// <summary>
    /// Called on client when transport raises an exception.</summary>
    /// </summary>
    /// <param name="exception">Exception thrown from the Transport.</param>
    public override void OnClientTransportException(Exception exception) { }

    #endregion

    #region Start & Stop Callbacks

    // Since there are multiple versions of StartServer, StartClient and StartHost, to reliably customize
    // their functionality, users would need override all the versions. Instead these callbacks are invoked
    // from all versions, so users only need to implement this one case.

    /// <summary>
    /// This is invoked when a host is started.
    /// <para>StartHost has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    public override void OnStartHost() { }

    /// <summary>
    /// This is invoked when a server is started - including when a host is started.
    /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    public override void OnStartServer()
    {
        logger.Log("[NewNetworkManager] OnStartServer called", this);
        // _ = worldLoader.LoadServer();
        base.OnStartServer();
    }

    /// <summary>
    /// This is invoked when the client is started.
    /// </summary>
    public override void OnStartClient()
    {
        logger.Log("[NewNetworkManager] OnStartClient called", this);
        base.OnStartClient();
    }

    /// <summary>
    /// This is called when a host is stopped.
    /// </summary>
    public override void OnStopHost() { }

    /// <summary>
    /// This is called when a server is stopped - including when a host is stopped.
    /// </summary>
    public override void OnStopServer() { }

    /// <summary>
    /// This is called when a client is stopped.
    /// </summary>
    public override void OnStopClient() { }

    #endregion


    /// <summary>
    ///  Add a player for the given connection.
    ///  This can be used on both the client and the server, but in different ways:
    ///  On the server, this function is called when a client connects and is ready.
    /// </summary>
    /// <param name="conn"></param>
    public GameObject AddPlayer()
    {
        if (playerPrefab == null)
        {
            logger.Log($"[NetworkManager] No player prefab found", this, Logging.LogType.Error);
            return null;
        }
        Transform startPos = GetStartPosition();
        // This is a workaround for the fact that Mirror's default implementation of
        // GetStartPosition returns null if no start positions are set,
        // which causes an error when trying to instantiate the player prefab.
        // We should setup the start position when worldInitializer works correctly
        Vector3 pos = startPos != null ? startPos.position : Vector3.zero;
        pos.y = 3f;

        Debug.Log($"Instantiating player prefab at position {pos}");
        GameObject player = containerScope.Container.Instantiate(
            playerPrefab,
            pos,
            Quaternion.Euler(Vector3.zero)
        );
        Debug.Log("Instantiated player prefab!");
        return player;
    }
}
