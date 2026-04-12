using FTR.Core.Client.EventChannels.Shop;
using FTR.Core.Common.Interactions;
using UnityEngine;

public class ShopInteractComponent : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ShopToggleEvent shopToggleEvent;

    private IInteractor _currentInteractor;

    private void OnEnable()
    {
        shopToggleEvent.OnRaised += OnShopToggled;
    }

    private void OnDisable()
    {
        shopToggleEvent.OnRaised -= OnShopToggled;
    }

    private void OnShopToggled(bool isOpen)
    {
        if (!isOpen)
            _currentInteractor?.FinishInteracting();
    }

    public string Interact(IInteractor interactor)
    {
        _currentInteractor = interactor;
        shopToggleEvent.Raise(true);
        return "";
    }

    public bool CanInteract(IInteractor interactor)
    {
        return true;
    }

    public void ContinueInteraction(IInteractor interactor) { }

    public void StopInteraction(IInteractor interactor) { }
}
