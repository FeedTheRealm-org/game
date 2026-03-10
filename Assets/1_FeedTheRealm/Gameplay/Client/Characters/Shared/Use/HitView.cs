using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

/// <summary>
/// Plays damaged and death animations when this character receives a HitEvent.
///
/// Routing path:
///   UseSystem (server) → HitEvent(targetNetId)
///     → FlushEventsToClients dispatches via target's NetworkAdapter
///       → target's NetworkEventRouter.OnHitEvent
///         → HitView.OnHitEvent → PlayDamaged() / PlayDeath()
/// </summary>
public class HitView : MonoBehaviour
{
    [SerializeField]
    private CharacterAnimator animator;

    private NetworkEventRouter eventRouter;

    public void Initialize(NetworkEventRouter eventRouter)
    {
        this.eventRouter = eventRouter;
        eventRouter.OnHitEvent += OnHitEvent;
    }

    private void OnDestroy()
    {
        if (eventRouter != null)
            eventRouter.OnHitEvent -= OnHitEvent;
    }

    private void OnHitEvent(HitEventContent hitContent)
    {
        if (hitContent.CurrentHealth <= 0f)
            animator.PlayDeath();
        else if (hitContent.CurrentHealth < hitContent.MaxHealth)
            animator.PlayDamaged();
    }
}
