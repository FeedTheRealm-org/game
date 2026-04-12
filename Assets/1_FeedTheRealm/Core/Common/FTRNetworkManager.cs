using System;
using System.Threading;
using System.Threading.Tasks;
using FTR.Core.Common.Config;
using FTR.Core.Common.EventChannels;
// using Core.Systems.Worlds;
using FTR.Core.Common.Loaders;
using FTR.Core.Common.Scopes;
// using FTRShared.Runtime.Models;
using kcp2k;
using Mirror;
// using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;
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
    InitiatePlayerEvent initiatePlayerEvent;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private Config config;

    private CancellationTokenSource worldLoadGateCts;

    /// <summary>
    /// Runs on both Server and Client
    /// Networking is NOT initialized when this fires
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        WorldLoadBootstrap.Reset();
        worldLoadGateCts?.Dispose();
        worldLoadGateCts = new CancellationTokenSource();
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
    public override async void Start()
    {
        logger.Log("[NetworkManager] Starting NetworkManager...", this);
        base.Start();

        KcpTransport kcp = Transport.active as KcpTransport;
        try
        {
            if (config.RuntimeRole == RuntimeRole.Server)
            {
                networkAddress = "0.0.0.0";
                kcp.Port = config.ListeningPort;

                logger.Log(
                    "[NetworkManager] Waiting for server world preload before accepting clients...",
                    this
                );
                var canStartServer = await WaitForWorldLoadGateAsync(
                    RuntimeRole.Server,
                    worldLoadGateCts.Token
                );
                if (!canStartServer)
                {
                    logger.Log(
                        "[NetworkManager] Server world preload failed, server will not start.",
                        this,
                        Logging.LogType.Error
                    );
                    Application.Quit();
                    return;
                }

                logger.Log($"[NetworkManager] Starting server on port {kcp.Port}", this);
                StartServer();
                NetworkSpawnPendingObjectsRegistry spawnerRegistry =
                    containerScope.Container.Resolve<NetworkSpawnPendingObjectsRegistry>();
                spawnerRegistry.SpawnAll();
            }
            else if (config.RuntimeRole == RuntimeRole.Client)
            {
                networkAddress = config.CurrentServerAddress;
                kcp.Port = config.CurrentServerPort;

                logger.Log(
                    "[NetworkManager] Waiting for client world preload before connecting...",
                    this
                );
                var canStartClient = await WaitForWorldLoadGateAsync(
                    RuntimeRole.Client,
                    worldLoadGateCts.Token
                );
                if (!canStartClient)
                {
                    logger.Log(
                        "[NetworkManager] Client world preload failed, connection was cancelled.",
                        this,
                        Logging.LogType.Error
                    );
                    return;
                }

                logger.Log(
                    $"[NetworkManager] Starting client, connecting to {networkAddress}:{kcp.Port}",
                    this
                );
                StartClient();
            }
        }
        catch (OperationCanceledException)
        {
            logger.Log(
                "[NetworkManager] World preload wait cancelled due to shutdown.",
                this,
                Logging.LogType.Warning
            );
        }
    }

    private async Task<bool> WaitForWorldLoadGateAsync(
        RuntimeRole runtimeRole,
        CancellationToken cancellationToken
    )
    {
        for (int i = 0; i < config.MaxWorldLoadRetries; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (runtimeRole == RuntimeRole.Server)
            {
                if (WorldLoadBootstrap.ServerReady)
                    return true;
                if (WorldLoadBootstrap.ServerFailed)
                    return false;
            }
            else if (runtimeRole == RuntimeRole.Client)
            {
                if (WorldLoadBootstrap.ClientReady)
                    return true;
                if (WorldLoadBootstrap.ClientFailed)
                    return false;
            }
            else
            {
                return true;
            }

            logger.Log(
                $"[NetworkManager] Waiting for world load {i + 1}/{config.MaxWorldLoadRetries}"
            );
            await Task.Delay(config.WorldLoadRetryDelayMs, cancellationToken);
        }
        return false;
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
        CancelWorldLoadGate();
        worldLoadGateCts?.Dispose();
        worldLoadGateCts = null;
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
        CancelWorldLoadGate();
        base.OnApplicationQuit();
    }

    private void CancelWorldLoadGate()
    {
        if (worldLoadGateCts == null || worldLoadGateCts.IsCancellationRequested)
            return;

        worldLoadGateCts.Cancel();
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
        GameObject player = AddPlayer();
        player.name = $"Player_{conn.connectionId}";

        // Add player to network - this will trigger OnStartServer on the player's NetworkBehaviour components
        NetworkServer.AddPlayerForConnection(conn, player);

        logger.Log(
            $"[NetworkManager] OnServerAddPlayer called for connection {conn.connectionId}",
            this
        );
    }

    public override Transform GetStartPosition()
    {
        return base.GetStartPosition();
    }

    /// <summary>
    /// Called on the server when a client disconnects.
    /// <para>This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        logger.Log(
            $"[NetworkManager] Client disconnected: connectionId={conn.connectionId}, address={conn.address}",
            this,
            Logging.LogType.Warning
        );
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
    )
    {
        logger.Log(
            $"[NetworkManager] Server transport error: {transportError}, connectionId={conn?.connectionId}, message={message}",
            this,
            Logging.LogType.Error
        );
    }

    /// <summary>
    /// Called on server when transport raises an exception.
    /// <para>NetworkConnection may be null.</para>
    /// </summary>
    /// <param name="conn">Connection of the client...may be null</param>
    /// <param name="exception">Exception thrown from the Transport.</param>
    public override void OnServerTransportException(
        NetworkConnectionToClient conn,
        Exception exception
    )
    {
        logger.Log(
            $"[NetworkManager] Server transport exception: connectionId={conn?.connectionId}, exception={exception}",
            this,
            Logging.LogType.Error
        );
    }

    #endregion

    #region Client System Callbacks

    /// <summary>
    /// Called on the client when connected to a server.
    /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
    /// </summary>
    public override void OnClientConnect()
    {
        logger.Log("[NetworkManager] Client connected to server", this);
        base.OnClientConnect();
    }

    /// <summary>
    /// Called on clients when disconnected from a server.
    /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
    /// </summary>
    public override void OnClientDisconnect()
    {
        logger.Log(
            $"[NetworkManager] Client disconnected from server. NetworkAddress={networkAddress}",
            this,
            Logging.LogType.Warning
        );
    }

    /// <summary>
    /// Called on clients when a servers tells the client it is no longer ready.
    /// <para>This is commonly used when switching scenes.</para>
    /// </summary>
    public override void OnClientNotReady()
    {
        logger.Log(
            "[NetworkManager] Client is no longer ready (server notified)",
            this,
            Logging.LogType.Warning
        );
    }

    /// <summary>
    /// Called on client when transport raises an error.</summary>
    /// </summary>
    /// <param name="transportError">TransportError enum.</param>
    /// <param name="message">String message of the error.</param>
    public override void OnClientError(TransportError transportError, string message)
    {
        logger.Log(
            $"[NetworkManager] Client transport error: {transportError}, message={message}",
            this,
            Logging.LogType.Error
        );
    }

    /// <summary>
    /// Called on client when transport raises an exception.</summary>
    /// </summary>
    /// <param name="exception">Exception thrown from the Transport.</param>
    public override void OnClientTransportException(Exception exception)
    {
        logger.Log(
            $"[NetworkManager] Client transport exception: {exception}",
            this,
            Logging.LogType.Error
        );
    }

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

        GameObject player = containerScope.Container.Instantiate(
            playerPrefab,
            pos,
            Quaternion.Euler(Vector3.zero)
        );
        return player;
    }
}
