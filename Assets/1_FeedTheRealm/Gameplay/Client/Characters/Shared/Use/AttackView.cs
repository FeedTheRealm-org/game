using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

public class AttackView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    private NetworkEventRouter eventRouter;

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

    private void OnAttackEvent(ServerEventDTO serverEvent)
    {
        animator.SetAction(true);
        animator.PlayAttack();
    }
}
