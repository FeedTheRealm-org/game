using System;
using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Config;
using FTR.Core.Common.Enums;
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
    private ReceivedActionCommandEvent receivedActionCommandEvent;

    [Inject]
    private ReceivedTransactionCommandEvent receivedTransactionCommandEvent;

    [Inject]
    private Config config;

    private Dictionary<ActionType, int> actionsSent = new Dictionary<ActionType, int>();
    private Dictionary<TransactionType, int> transactionsSent =
        new Dictionary<TransactionType, int>();
    private Coroutine actionLogRoutine;

    private void Start()
    {
        if (isLocalPlayer && config.EnableActionLogging)
            actionLogRoutine = StartCoroutine(ShowActionLog());
    }

    private void OnDestroy()
    {
        if (actionLogRoutine != null)
            StopCoroutine(actionLogRoutine);
    }

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

        if (!actionsSent.ContainsKey(command.Type))
            actionsSent[command.Type] = 0;
        actionsSent[command.Type]++;

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

        if (!transactionsSent.ContainsKey(command.Type))
            transactionsSent[command.Type] = 0;
        transactionsSent[command.Type]++;

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

        if (targetConnectionId.HasValue)
        {
            if (
                NetworkServer.connections.TryGetValue(
                    targetConnectionId.Value,
                    out var targetConnection
                )
            )
                TargetRpcServerEvent(targetConnection, response);
            else
                logger.Log($"Failed to dispatch Targeted Server Event for NetId: {netId}", this);
        }
        else
        {
            RpcServerEvent(response);
        }
    }

    /// <summary>
    /// DisconnectClient is called by the server to disconnect a client.
    /// Server ONLY.
    /// </summary>
    [Server]
    public void DisconnectClient()
    {
        if (!isServer)
            return;

        if (
            NetworkServer.connections.TryGetValue(
                connectionToClient.connectionId,
                out var targetConnection
            )
        )
            targetConnection.Disconnect();
        else
            logger.Log($"Failed to disconnect client for NetId: {netId}", this);
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
        OnServerEvent?.Invoke(response);
    }

    private IEnumerator ShowActionLog()
    {
        while (true)
        {
            logger.Log("=== Action Log ===");
            foreach (var action in actionsSent)
                logger.Log($"Action: {action.Key}, Times Sent: {action.Value}");
            logger.Log("=== Transaction Log ===");
            foreach (var transaction in transactionsSent)
                logger.Log($"Transaction: {transaction.Key}, Times Sent: {transaction.Value}");
            logger.Log("======================");
            yield return new WaitForSeconds(5f);
        }
    }
}
