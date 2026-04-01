using FTR.Core.Client.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using VContainer;

public class ShopView : MonoBehaviour
{
    [Inject]
    private OpenShopEvent openShopEvent;

    private NetworkEventRouter eventRouter;

    public void Initialize(NetworkEventRouter eventRouter)
    {
        this.eventRouter = eventRouter;
        eventRouter.OnOpenShopEvent += HandleOpenShopEvent;

        Debug.Log($"[ShopView] Initialized. eventRouter set: {eventRouter != null}");
    }

    private void OnDestroy()
    {
        if (eventRouter != null)
            eventRouter.OnOpenShopEvent -= HandleOpenShopEvent;
    }

    private void HandleOpenShopEvent(OpenShopEventContent content)
    {
        Debug.Log($"[ShopView] HandleOpenShopEvent received.");
        openShopEvent.Raise();
    }
}
