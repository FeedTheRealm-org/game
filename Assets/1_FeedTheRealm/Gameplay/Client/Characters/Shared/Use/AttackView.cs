using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Registry;
using UnityEngine;
using VContainer;

public class AttackView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    [SerializeField]
    [Range(0f, 2f)]
    [Tooltip(
        "Delay in seconds befor starting to reproduce audio. "
            + "Change to adjust timing with animation."
    )]
    private float attackSoundDelay = 0.75f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Initial sound for attack sound effect.")]
    private float attackSoundVolume = 1f;

    private NetworkEventRouter eventRouter;
    private IAudioManager audioManager;

    [Inject]
    public void Construct(IAudioManager audioManager)
    {
        this.audioManager = audioManager;
    }

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

        audioManager.PlaySoundFXById(
            ClientSoundFXRegistry.SoundFXIds.Attack,
            transform.position,
            priority: 64f,
            delay: attackSoundDelay,
            volume: attackSoundVolume
        );
    }
}
