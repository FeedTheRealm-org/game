using FTR.Core.Client.EventChannels;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.Shop
{
    [RequireComponent(typeof(UIDocument))]
    public class ShopUIController : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        private OpenShopEvent openShopEvent;

        private VisualElement _shopPanel;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _shopPanel = root.Q<VisualElement>("Shop");

            if (_shopPanel == null)
            {
                logger.Log(
                    "[ShopUIController] Shop element not found in UIDocument.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            SetVisible(false);

            openShopEvent.OnRaised += OnOpenShop;
        }

        private void OnDisable()
        {
            openShopEvent.OnRaised -= OnOpenShop;
        }

        private void OnOpenShop()
        {
            bool show = _shopPanel.style.display == DisplayStyle.None;
            SetVisible(show);
        }

        private void SetVisible(bool visible)
        {
            if (_shopPanel == null)
                return;

            _shopPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
