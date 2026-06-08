using System;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;

public class LootItemView : MonoBehaviour
{
    private Rigidbody rb;
    private NetworkEventRouter eventRouter;
    private LootItemStateStorage stateStorage;
    private bool isGrounded = false;

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
        this.stateStorage.OnGrounded += OnGrounded;
    }

    private void OnPositionCorrected(Vector3 targetPosition)
    {
        rb.position = targetPosition;
        if (isGrounded)
            rb.constraints = RigidbodyConstraints.FreezeAll;
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
        Debug.Log(
            $"[LootItemView] Received InitialForceEventContent for LootItem {stateStorage.ItemId}. Applying force correction.",
            this
        );
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

    private void OnGrounded()
    {
        isGrounded = true;
    }
}
