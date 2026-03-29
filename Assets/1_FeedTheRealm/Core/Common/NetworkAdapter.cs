using System;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using Mirror;
using UnityEngine;
using VContainer;

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
    public event Action<ServerEventDTO> OnServerEvent;

    // Server ONLY
    public event Action<ActionCommandDTO> OnActionRequest;
    public event Action<TransactionCommandDTO> OnTransactionRequest;

    public bool IsLocalPlayer => isLocalPlayer;

    [Inject]
    ReceivedActionCommandEvent receivedActionCommandEvent;

    [Inject]
    ReceivedTransactionCommandEvent receivedTransactionCommandEvent;

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

        /*logger.Log(
            $"Dispatching Action Command: {command.Type} with direction {command.Direction}",
            this
        );*/

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
    /// DispatchEvent is called by the server to dispatch a server response to all clients or a targeted one.
    /// Server ONLY.
    /// </summary>
    [Server]
    public void DispatchEvent(ServerEventDTO response, int? targetConnectionId = null)
    {
        if (!isServer)
            return;

        Debug.Log(
            $"[NetworkAdapter] DispatchEvent called | netId:{netId} | "
                + $"connectionToClient:{connectionToClient?.connectionId.ToString() ?? "null"} | "
                + $"targetConnectionId:{targetConnectionId}"
        );
        if (targetConnectionId.HasValue)
        {
            if (
                NetworkServer.connections.TryGetValue(
                    targetConnectionId.Value,
                    out var targetConnection
                )
            )
            {
                logger.Log(
                    $"Dispatching Targeted Server Event: {response.Type} for NetId: {netId} to connection {targetConnectionId}",
                    this
                );
                TargetRpcServerEvent(targetConnection, response);
            }
            else
            {
                logger.Log(
                    $"Failed to dispatch Targeted Server Event: {response.Type} for NetId: {netId} to connection {targetConnectionId} - connection not found",
                    this
                );
            }
        }
        else
        {
            logger.Log(
                $"Dispatching Broadcast Server Event: {response.Type} for NetId: {netId}",
                this
            );
            RpcServerEvent(response);
        }
    }

    /* --- RPCs --- */

    /// <summary>
    /// CmdActionRequest is a server command method that dispatches the action to the server for processing.
    /// </summary>
    [Command(channel = Channels.Reliable)]
    private void CmdActionRequest(ActionCommandDTO command)
    {
        command.NetId = netId;
        receivedActionCommandEvent.Raise(command);
    }

    /// <summary>
    /// CmdTransactionRequest is a server command method that dispatches the transaction to the server for processing.
    /// </summary>
    [Command(channel = Channels.Reliable)]
    private void CmdTransactionRequest(TransactionCommandDTO command)
    {
        command.NetId = netId;
        logger.Log($"Received Transaction Command from client: {command.NetId}", this);
        receivedTransactionCommandEvent.Raise(command);
    }

    /// <summary>
    /// RpcDispatchResponse is a client RPC method that dispatches a server response to all clients via event.
    /// </summary>
    [ClientRpc(channel = Channels.Reliable)]
    private void RpcServerEvent(ServerEventDTO response)
    {
        OnServerEvent?.Invoke(response);
    }

    /// <summary>
    /// TargetServerEvent is a client RPC method that dispatches a server response to a single targeted client via event.
    /// </summary>
    [TargetRpc(channel = Channels.Reliable)]
    private void TargetRpcServerEvent(NetworkConnectionToClient target, ServerEventDTO response)
    {
        Debug.Log(
            $"[NetworkAdapter CLIENT] TargetRpc received | type:{response.Type} | localNetId:{netId}"
        );
        OnServerEvent?.Invoke(response);
    }
}
