using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Registry;
using UnityEngine;
using VContainer;

public class AttackView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    private NetworkEventRouter eventRouter;

    [Inject]
    private ISoundPlayer soundPlayer;

    public void Initialize(NetworkEventRouter eventRouter)
    {
        this.eventRouter = eventRouter;
        eventRouter.OnAttackEvent += OnAttackEvent;
    }

    private void OnDestroy()
    {
        if (eventRouter != null)
            eventRouter.OnAttackEvent -= OnAttackEvent;
    }

    private void OnAttackEvent(AttackEventContent attackEvent)
    {
        animator.SetAction(true);
        animator.PlayAttack();

        soundPlayer.Play(ClientSoundFXRegistry.SoundFXIds.Attack, transform.position);
    }
}
