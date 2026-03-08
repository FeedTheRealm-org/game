using Cysharp.Threading.Tasks;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class DashView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    private Rigidbody rb;

    private NetworkEventRouter eventRouter;

    public void Initialize(Rigidbody rb, NetworkEventRouter eventRouter)
    {
        this.rb = rb;
        this.eventRouter = eventRouter;
        eventRouter.OnDashEvent += OnDashEvent;
    }

    private void OnDestroy()
    {
        if (eventRouter != null)
            eventRouter.OnDashEvent -= OnDashEvent;
    }

    private async void OnDashEvent(DashEventContent dashEvent)
    {
        Vector3 force = new Vector3(dashEvent.Force.X, dashEvent.Force.Y, dashEvent.Force.Z);
        int duration = (int)dashEvent.Duration * 1000;

        ApplyDashing(force); // apply instant burst
        await UniTask.Delay(duration);
        StopDash(); // stop dash instantly for "snappy" feel;
    }

    private void ApplyDashing(Vector3 force)
    {
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.VelocityChange);
    }

    private void StopDash()
    {
        rb.linearVelocity = Vector3.zero;
    }
}
