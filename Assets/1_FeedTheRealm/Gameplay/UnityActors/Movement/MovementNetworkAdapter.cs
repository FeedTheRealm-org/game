using System;
using Game.Core.Domain.Movement;
using Game.Core.Server.Movement;
using Mirror;
using UnityEngine;

public class MovementNetworkAdapter : NetworkBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float sendInterval = 0.05f;

    [Header("Debug")]
    [SerializeField]
    private Logging.Logger logger;

    public event Action<MovementSnapshot> OnMovementReconcileSnapshot;

    private float nextSendTime = 0f;

    public void Tick(MovementCommand command)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        // TODO: optimize message sending (send states not positions, e.g. moving in X direction)
        // if (Time.time >= nextSendTime)
        nextSendTime = Time.time + sendInterval;
        CmdMovementRequest(command);
    }

    [Command(channel = Channels.Unreliable)]
    private void CmdMovementRequest(MovementCommand command)
    {
        var snapshot = ServerMovementSystem.ProcessMovementCommand(
            transform,
            command,
            moveSpeed,
            Time.fixedDeltaTime
        );

        // Update position if moving player is not host (as host is moved by view)
        bool isHostPlayer =
            NetworkServer.localConnection != null
            && connectionToClient == NetworkServer.localConnection;
        if (!isHostPlayer)
            transform.position = new Vector3(snapshot.x, snapshot.y, snapshot.z);

        RpcMovementResponse(snapshot);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    private void RpcMovementResponse(MovementSnapshot snapshot)
    {
        if (isLocalPlayer)
            OnMovementReconcileSnapshot.Invoke(snapshot);
        else
            transform.position = new Vector3(snapshot.x, snapshot.y, snapshot.z);
    }
}
