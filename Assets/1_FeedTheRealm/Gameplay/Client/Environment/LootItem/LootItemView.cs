using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class LootItemView : MonoBehaviour
{
    private Rigidbody rb;
    private NetworkEventRouter eventRouter;

    public void Initialize(Rigidbody rb, NetworkEventRouter eventRouter)
    {
        this.rb = rb;
        this.eventRouter = eventRouter;
        this.eventRouter.OnLootItemSpawnEvent += OnInitialForceCorrection;
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
        Vector3 initialPosition = new Vector3(
            initialForceEventContent.InitialPosition.X,
            initialForceEventContent.InitialPosition.Y,
            initialForceEventContent.InitialPosition.Z
        );
        rb.MovePosition(initialPosition);
        Vector3 force = new Vector3(
            initialForceEventContent.Force.X,
            initialForceEventContent.Force.Y,
            initialForceEventContent.Force.Z
        );
        rb.AddForce(force, ForceMode.VelocityChange);
    }
}
