using FTR.Core.Client.EventChannels;
using FTR.Core.Common.Interactions;
using UnityEngine;

public class ShopInteractComponent : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ShopInteractedEvent shopInteractedEvent;

    [SerializeField]
    private ShopOnCloseEvent shopOnCloseEvent;

    private IInteractor _currentInteractor;

    private void OnEnable()
    {
        shopOnCloseEvent.OnRaised += OnShopClose;
    }

    private void OnDisable()
    {
        shopOnCloseEvent.OnRaised -= OnShopClose;
    }

    private void OnShopClose()
    {
        _currentInteractor?.FinishInteracting();
    }

    public string Interact(IInteractor interactor)
    {
        _currentInteractor = interactor;
        shopInteractedEvent.Raise();
        return "";
    }

    public bool CanInteract(IInteractor interactor)
    {
        return true;
    }
}
