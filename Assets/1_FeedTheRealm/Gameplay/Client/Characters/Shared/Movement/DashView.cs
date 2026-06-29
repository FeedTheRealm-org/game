using System.Threading;
using Cysharp.Threading.Tasks;
using FTR.Core.Client;
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

    [Inject]
    private ClientPrefabProvider prefabProvider;
    private GameObject dashEffectInstance;
    private CancellationTokenSource dashEffectCts;

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
        SetUpVFX();
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
        PlayDashEffect();

        soundPlayer.Play(ClientSoundFXRegistry.SoundFXIds.Dash, transform.position);

        await UniTask.Delay(duration);

        StopDash();
        stateStorage.IsMovementBlocked = false;
    }

    private void ApplyDashing(Vector3 force)
    {
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.VelocityChange);
        dashEffectInstance.SetActive(true);
        dashEffectInstance.GetComponent<ParticleSystem>().Play();
    }

    private void StopDash()
    {
        rb.linearVelocity = Vector3.zero;
    }

    private void SetUpVFX()
    {
        dashEffectInstance = Instantiate(prefabProvider.DashEffectPrefab, transform);
        dashEffectInstance.transform.localPosition = new Vector3(0, -1f, -0.5f);
        dashEffectInstance.transform.localScale = new Vector3(3f, 3f, 3f);
        dashEffectInstance.SetActive(false);
    }

    private void PlayDashEffect()
    {
        dashEffectCts?.Cancel();
        dashEffectCts = new CancellationTokenSource();

        RunDashEffect(dashEffectCts.Token).Forget();
    }

    private async UniTaskVoid RunDashEffect(CancellationToken token)
    {
        dashEffectInstance.SetActive(true);

        var ps = dashEffectInstance.GetComponent<ParticleSystem>();
        ps.Play();

        try
        {
            await UniTask.Delay(2000, cancellationToken: token);
        }
        catch
        {
            return;
        }

        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        await UniTask.Delay(500);

        dashEffectInstance.SetActive(false);
    }
}
