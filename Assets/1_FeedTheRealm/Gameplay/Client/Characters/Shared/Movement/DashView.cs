using Cysharp.Threading.Tasks;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

public class DashView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    [Inject]
    private ISoundPlayer soundPlayer;

    private Rigidbody rb;
    private CharacterStateStorage stateStorage;
    private NetworkEventRouter eventRouter;

    public void Initialize(
        Rigidbody rb,
        CharacterStateStorage stateStorage,
        NetworkEventRouter eventRouter
    )
    {
        this.rb = rb;
        this.stateStorage = stateStorage;
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
        int duration = (int)(dashEvent.Duration * 1000);

        stateStorage.IsMovementBlocked = true;
        ApplyDashing(force);
        soundPlayer.Play(ClientSoundFXRegistry.SoundFXIds.Dash, transform.position);
        await UniTask.Delay(duration);
        StopDash();
        stateStorage.IsMovementBlocked = false;
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
