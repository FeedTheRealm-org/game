using System;
using Game.Core.Domain.Movement;
using Game.Core.Server.Movement;
using Mirror;
using UnityEngine;

namespace Game.Gameplay.UnityActors.Movement;

public class MovementNetworkAdapter : NetworkBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float sendInterval = 0.05f;

    public event Action<MovementSnapshot> OnMovementReconcileSnapshot;

    private float nextSendTime = 0f;

    public void Tick(MovementCommand command)
    {
        if (!isOwned)
            return;

        if (Time.time >= nextSendTime)
        {
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
