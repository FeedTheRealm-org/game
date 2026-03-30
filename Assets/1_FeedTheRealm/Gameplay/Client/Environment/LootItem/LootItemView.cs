using System;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;

public class LootItemView : MonoBehaviour
{
    private Rigidbody rb;
    private NetworkEventRouter eventRouter;
    private LootItemStateStorage stateStorage;

    public void Initialize(
        Rigidbody rb,
        NetworkEventRouter eventRouter,
        LootItemStateStorage stateStorage
    )
    {
        this.rb = rb;
        this.eventRouter = eventRouter;
        this.stateStorage = stateStorage;
        this.eventRouter.OnLootItemSpawnEvent += OnInitialForceCorrection;
        this.stateStorage.OnPositionCorrected += OnPositionCorrected;
    }

    private void OnPositionCorrected(Vector3 targetPosition)
    {
        Debug.Log($"[LootItemView] Position corrected to {targetPosition}", this);
        rb.position = targetPosition;
        rb.constraints = RigidbodyConstraints.FreezePosition;
    }

    private void OnDisable()
    {
        eventRouter.OnLootItemSpawnEvent -= OnInitialForceCorrection;
    }

    /// <summary>
    /// OnInitialForceCorrection is used for reconciliation and error correction,
    /// </summary>
    private void OnInitialForceCorrection(InitialForceEventContent initialForceEventContent)
    {
        Vector3 initialPosition = new(
            initialForceEventContent.InitialPosition.X,
            initialForceEventContent.InitialPosition.Y,
            initialForceEventContent.InitialPosition.Z
        );
        Vector3 force = new(
            initialForceEventContent.Force.X,
            initialForceEventContent.Force.Y,
            initialForceEventContent.Force.Z
        );
        rb.MovePosition(initialPosition);
        rb.AddForce(force, ForceMode.VelocityChange);
    }
}
