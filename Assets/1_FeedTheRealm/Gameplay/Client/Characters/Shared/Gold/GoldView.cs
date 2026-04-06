using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Gold;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Common.NetworkEntities.Gold;
using UnityEngine;
using VContainer;

/// <summary>
/// Tracks updates on the local player's gold and notifies the HUD via event channels.
/// </summary>
public class GoldView : MonoBehaviour
{
    [Inject]
    [SerializeField]
    private GoldChangedEvent goldChangedEvent;

    [Inject]
    private NotEnoughGoldEvent notEnoughGoldEvent;

    [SerializeField]
    private GoldStateStorage stateStorage;
    private NetworkEventRouter eventRouter;

    public void Initialize(GoldStateStorage stateStorage, NetworkEventRouter eventRouter)
    {
        this.stateStorage = stateStorage;
        this.eventRouter = eventRouter;
        stateStorage.OnGoldChanged += OnGoldChanged;
        eventRouter.OnNotEnoughGoldEvent += HandleNotEnoughGoldEvent;
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
            stateStorage.OnGoldChanged -= OnGoldChanged;
        if (eventRouter != null)
            eventRouter.OnNotEnoughGoldEvent -= HandleNotEnoughGoldEvent;
    }

    private void OnGoldChanged(int newGold)
    {
        Debug.Log($"GoldView gold changed: {newGold}");
        goldChangedEvent.Raise(newGold);
    }

    private void HandleNotEnoughGoldEvent(NotEnoughGoldEventContent content)
    {
        Debug.Log($"GoldView received NotEnoughGoldEvent for required amount: {content.Amount}");
        notEnoughGoldEvent.Raise((content.ProductId, content.Amount));
    }
}
