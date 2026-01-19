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
        if (!isOwned)
        {
            logger.Log(
                "Tick called on non-owned MovementNetworkAdapter",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        if (Time.time >= nextSendTime)
        {
            logger.Log($"Sending MovementCommand Seq {command.sequenceNumber}", this);
            nextSendTime = Time.time + sendInterval;
            CmdMovementRequest(command);
        }
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

        RpcMovementResponse(snapshot);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    private void RpcMovementResponse(MovementSnapshot snapshot)
    {
        if (isOwned)
            OnMovementReconcileSnapshot.Invoke(snapshot);
        else
            transform.position = new Vector3(snapshot.x, snapshot.y, snapshot.z);
    }
}
