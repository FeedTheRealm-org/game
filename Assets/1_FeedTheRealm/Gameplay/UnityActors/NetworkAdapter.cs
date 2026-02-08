using System;
using Game.Core.RpcMessages;
using Mirror;
using UnityEngine;

/// <summary>
/// NetworkAdapter is responsible for handling all gameplay network communication between clients and the server.
/// Action, Transactions or Responses are dispatched by the local actor through this adapter.
/// Requests or responses are received via events.
/// </summary>
public class NetworkAdapter : NetworkBehaviour
{
    [Header("General Settings")]
    [SerializeField]
    private Logging.Logger logger;

    // Client ONLY
    public event Action<ServerResponseDTO> OnServerResponse;

    // Server ONLY
    public event Action<ActionCommandDTO> OnActionRequest;
    public event Action<TransactionCommandDTO> OnTransactionRequest;

    // [Inject] ServerWorld world;

    // public override void OnStartServer()
    // {
    //     var entity = new ServerEntity(
    //         netId,
    //         this,
    //         GetComponent<MovementController>()
    //     );

    //     world.Entities.Register(netId, entity);
    // }

    /* --- DISPATCHERS --- */

    /// <summary>
    /// DispatchAction is called by the local player to dispatch an action command to the server for processing.
    /// Client ONLY.
    /// </summary>
    [Client]
    public void DispatchAction(ActionCommandDTO command)
    {
        if (!isLocalPlayer)
            return;

        CmdActionRequest(command);
    }

    /// <summary>
    /// DispatchTransaction is called by the local player to dispatch a transaction command to the server for processing.
    /// Client ONLY.
    /// </summary>
    [Client]
    public void DispatchTransaction(TransactionCommandDTO command)
    {
        if (!isLocalPlayer)
            return;

        CmdTransactionRequest(command);
    }

    /// <summary>
    /// DispatchResponse is called by the server to dispatch a server response to all clients.
    /// Server ONLY.
    /// </summary>
    [Server]
    public void DispatchResponse(ServerResponseDTO response)
    {
        if (!isServer)
            return;

        RpcServerResponse(response);
    }

    /* --- RPCs --- */

    /// <summary>
    /// CmdActionRequest is a server command method that dispatches the action to the server for processing.
    /// </summary>
    [Command(channel = Channels.Reliable)]
    private void CmdActionRequest(ActionCommandDTO command)
    {
        // var snapshot = ServerMovementSystem.ProcessMovementCommand(
        //     rb,
        //     command,
        //     moveSpeed,
        //     Time.fixedDeltaTime
        // );

        // // Update position if moving player is not host (as host is moved by view)
        // bool isHostPlayer =
        //     NetworkServer.localConnection != null
        //     && connectionToClient == NetworkServer.localConnection;
        // if (!isHostPlayer)
        //     rb.MovePosition(new Vector3(snapshot.x, snapshot.y, snapshot.z));

        // RpcMovementResponse(snapshot);
    }

    /// <summary>
    /// CmdTransactionRequest is a server command method that dispatches the transaction to the server for processing.
    /// </summary>
    [Command(channel = Channels.Reliable)]
    private void CmdTransactionRequest(TransactionCommandDTO command)
    {
        // var snapshot = ServerMovementSystem.ProcessMovementCommand(
        //     rb,
        //     command,
        //     moveSpeed,
        //     Time.fixedDeltaTime
        // );

        // // Update position if moving player is not host (as host is moved by view)
        // bool isHostPlayer =
        //     NetworkServer.localConnection != null
        //     && connectionToClient == NetworkServer.localConnection;
        // if (!isHostPlayer)
        //     rb.MovePosition(new Vector3(snapshot.x, snapshot.y, snapshot.z));

        // RpcMovementResponse(snapshot);
    }

    /// <summary>
    /// RpcDispatchResponse is a client RPC method that dispatches a server response to all clients via event.
    /// </summary>
    [ClientRpc(channel = Channels.Reliable)]
    private void RpcServerResponse(ServerResponseDTO response)
    {
        OnServerResponse?.Invoke(response);
    }
}
