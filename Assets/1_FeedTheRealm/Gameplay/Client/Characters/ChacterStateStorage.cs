using System;
using Mirror;
using UnityEngine;

public class CharacterStateStorage : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnPositionSync))]
    private Vector3 position;

    [SyncVar]
    private Vector3 direction;

    [SyncVar]
    private Vector3 velocity;

    /* --- Getters --- */

    public Vector3 Position => position;
    public Vector3 Direction => direction;
    public Vector3 Velocity => velocity;

    public event Action<Vector3> OnPositionCorrected;

    /* --- Setters --- */

    [Server]
    public void SetMovement(Vector3 newDirection, Vector3 newVelocity)
    {
        direction = newDirection;
        velocity = newVelocity;
    }

    [Server]
    public void CorrectPosition(Vector3 newPosition)
    {
        position = newPosition;
    }

    /* --- Syncvar hooks --- */

    private void OnPositionSync(Vector3 oldPosition, Vector3 newPosition)
    {
        OnPositionCorrected?.Invoke(newPosition);
    }
}
