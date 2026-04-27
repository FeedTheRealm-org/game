using System.Collections;
using System.Collections.Generic;
using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Gold;
using FTR.Gameplay.Client.Registry;
using FTR.UI.Inventory;
using FTRShared.Runtime.Models;
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

        [SerializeField]
        private API.ItemAssetsService itemAssetsService;

        [SerializeField]
        private ItemStatsTooltip itemStatsTooltipPrefab;

        [Inject]
        private OpenShopEvent openShopEvent;

        [Inject]
        private PurchaseRequestEvent purchaseRequestEvent;

        [Inject]
        private NotEnoughGoldEvent notEnoughGoldEvent;

        private VisualElement _shopPanel;
        private VisualElement _closeButton;
        private VisualElement _shopItemsContainer;
        private Label _notEnoughGoldLabel;

        private ItemStatsTooltip itemStatsTooltip;
        private Coroutine _hideMessageCoroutine;

        private void OnEnable()
        {
            if (itemStatsTooltip == null && itemStatsTooltipPrefab != null)
                itemStatsTooltip = Instantiate(itemStatsTooltipPrefab);

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

            _closeButton = _shopPanel.Q<VisualElement>("CloseButton");
            if (_closeButton == null)
            {
                logger.Log(
                    "[ShopUIController] CloseButton element not found inside Shop.",
                    this,
                    Logging.LogType.Error
                );
            }
            else
            {
                _closeButton.RegisterCallback<ClickEvent>(_ => SetVisible(false));
            }

            _shopItemsContainer = _shopPanel.Q<VisualElement>("ShopItemsContainer");
            if (_shopItemsContainer == null)
            {
                logger.Log(
                    "[ShopUIController] ShopItemsContainer element not found inside Shop.",
                    this,
                    Logging.LogType.Error
                );
            }

            SetupNotEnoughGoldLabel();
            SetVisible(false);

            openShopEvent.OnRaised += OnOpenShop;
            notEnoughGoldEvent.OnRaised += OnNotEnoughGold;
        }

        private void OnDisable()
        {
            openShopEvent.OnRaised -= OnOpenShop;
            notEnoughGoldEvent.OnRaised -= OnNotEnoughGold;
        }

        private void SetupNotEnoughGoldLabel()
        {
            var panel = _shopPanel.Q<VisualElement>("Panel");
            if (panel == null)
            {
                logger.Log(
                    "[ShopUIController] Panel element not found inside Shop.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            _notEnoughGoldLabel = new Label();
            _notEnoughGoldLabel.AddToClassList("shop-not-enough-gold-message");
            _notEnoughGoldLabel.style.display = DisplayStyle.None;

            // Insert above CloseButton (second-to-last position)
            panel.Insert(panel.childCount - 1, _notEnoughGoldLabel);
        }

        private void OnNotEnoughGold((string productId, int amount) data)
        {
            if (_notEnoughGoldLabel == null)
                return;

            var itemData = ClientItemsRegistry.GetItemById(data.productId);
            string itemName = itemData != null ? itemData.name : data.productId;

            _notEnoughGoldLabel.text = $"Not enough gold to buy {itemName} x{data.amount}!";
            _notEnoughGoldLabel.style.display = DisplayStyle.Flex;

            if (_hideMessageCoroutine != null)
                StopCoroutine(_hideMessageCoroutine);

            _hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay(3f));
        }

        private IEnumerator HideMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_notEnoughGoldLabel != null)
                _notEnoughGoldLabel.style.display = DisplayStyle.None;

            _hideMessageCoroutine = null;
        }

        private void OnOpenShop(string shopId)
        {
            bool show = _shopPanel.style.display == DisplayStyle.None;

            if (show)
                PopulateShop(shopId);

            SetVisible(show);
        }

        private void PopulateShop(string shopId)
        {
            if (_shopItemsContainer == null)
                return;

            _shopItemsContainer.Clear();

            if (!ClientShopRegistry.TryGetShop(shopId, out ShopData shopData))
            {
                logger.Log(
                    $"[ShopUIController] Shop '{shopId}' not found in ClientShopRegistry.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            List<ProductData> products = shopData.products;
            if (products == null || products.Count == 0)
            {
                logger.Log(
                    $"[ShopUIController] Shop '{shopId}' has no products.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            foreach (var product in products)
            {
                var row = new VisualElement();
                row.AddToClassList("shop-item");

                var icon = new VisualElement();
                icon.AddToClassList("shop-item-icon");
                SlotItemLoader.LoadItem(icon, product.productId, itemAssetsService);

                var nameLabel = new Label(product.productId);
                nameLabel.AddToClassList("shop-item-name");
                var itemData = ClientItemsRegistry.GetItemById(product.productId);
                if (itemData != null)
                    nameLabel.text = itemData.name;

                var priceLabel = new Label($"{product.price} {product.currency}");
                priceLabel.AddToClassList("shop-item-price");

                var amountField = new IntegerField();
                amountField.AddToClassList("shop-item-amount");
                amountField.value = 1;
                amountField.RegisterCallback<ChangeEvent<int>>(evt =>
                {
                    if (evt.newValue < 1)
                        amountField.SetValueWithoutNotify(1);
                });

                var buyButton = new VisualElement();
                buyButton.AddToClassList("shop-buy-button");
                var buyLabel = new Label("Buy");
                buyLabel.AddToClassList("shop-buy-button-label");
                buyButton.Add(buyLabel);

                string capturedId = product.productId;
                buyButton.RegisterCallback<ClickEvent>(_ =>
                {
                    int amount = Mathf.Max(1, amountField.value);
                    Debug.Log($"[ShopUIController] Buying {amount}x '{capturedId}'");
                    purchaseRequestEvent?.Raise((capturedId, amount));
                });

                row.Add(icon);
                row.Add(nameLabel);
                row.Add(priceLabel);
                row.Add(amountField);
                row.Add(buyButton);

                icon.RegisterCallback<PointerEnterEvent>(_ =>
                    itemStatsTooltip?.ShowTooltip(capturedId, icon)
                );
                icon.RegisterCallback<PointerLeaveEvent>(_ => itemStatsTooltip?.HideTooltip());

                _shopItemsContainer.Add(row);
            }

            logger.Log(
                $"[ShopUIController] Populated shop '{shopId}' with {products.Count} products.",
                this
            );
        }

        private void SetVisible(bool visible)
        {
            if (_shopPanel == null)
                return;

            _shopPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
