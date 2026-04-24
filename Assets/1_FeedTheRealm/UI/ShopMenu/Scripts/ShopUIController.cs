using System.Collections;
using System.Collections.Generic;
using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Gold;
using FTR.Core.Client.EventChannels.Shop;
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
        private ShopToggleEvent shopToggleEvent;

        [Inject]
        private PurchaseRequestEvent purchaseRequestEvent;

        [Inject]
        private NotEnoughGoldEvent notEnoughGoldEvent;

        // Root
        private VisualElement _shopRoot;
        private VisualElement _panel;

        // Header
        private VisualElement _closeButton;

        // Tabs
        private VisualElement _tabGold;
        private VisualElement _tabCosmetic;
        private Label _tabGoldLabel;
        private Label _tabCosmeticLabel;

        // Tab content
        private VisualElement _tabGoldContent;
        private VisualElement _tabCosmeticContent;

        // Item containers
        private VisualElement _shopItemsContainer; // Gold items
        private VisualElement _cosmeticItemsContainer; // Cosmetic items (future use)

        // Message area
        private VisualElement _messageArea;
        private Label _notEnoughGoldLabel;

        // State
        private bool _isGoldTabActive = true;
        private ItemStatsTooltip _itemStatsTooltip;
        private Coroutine _hideMessageCoroutine;
        private Coroutine _animationCoroutine;

        // Animation settings
        private const float AnimationDuration = 0.25f;

        // ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_itemStatsTooltip == null && itemStatsTooltipPrefab != null)
                _itemStatsTooltip = Instantiate(itemStatsTooltipPrefab);

            var root = GetComponent<UIDocument>().rootVisualElement;

            _shopRoot = root.Q<VisualElement>("Shop");
            if (_shopRoot == null)
            {
                logger.Log(
                    "[ShopUIController] 'Shop' element not found.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            _panel = _shopRoot.Q<VisualElement>("Panel");

            // Header
            _closeButton = _shopRoot.Q<VisualElement>("CloseButton");
            if (_closeButton != null)
            {
                _closeButton.RegisterCallback<ClickEvent>(_ => CloseShop());
            }

            // Tabs
            _tabGold = _shopRoot.Q<VisualElement>("TabGold");
            _tabCosmetic = _shopRoot.Q<VisualElement>("TabCosmetic");
            _tabGoldLabel = _shopRoot.Q<Label>("TabGoldLabel");
            _tabCosmeticLabel = _shopRoot.Q<Label>("TabCosmeticLabel");

            if (_tabGold != null)
                _tabGold.RegisterCallback<ClickEvent>(_ => SwitchTab(true));

            if (_tabCosmetic != null)
                _tabCosmetic.RegisterCallback<ClickEvent>(_ => SwitchTab(false));

            // Tab contents
            _tabGoldContent = _shopRoot.Q<VisualElement>("TabGoldContent");
            _tabCosmeticContent = _shopRoot.Q<VisualElement>("TabCosmeticContent");

            // Item containers
            _shopItemsContainer = _shopRoot.Q<VisualElement>("ShopItemsContainer");
            _cosmeticItemsContainer = _shopRoot.Q<VisualElement>("CosmeticItemsContainer");

            // Message area
            _messageArea = _shopRoot.Q<VisualElement>("MessageArea");
            SetupNotEnoughGoldLabel();

            // Close backdrop click
            _shopRoot.RegisterCallback<ClickEvent>(OnBackdropClick);

            SetVisible(false, instant: true);

            openShopEvent.OnRaised += OnOpenShop;
            notEnoughGoldEvent.OnRaised += OnNotEnoughGold;
        }

        private void OnDisable()
        {
            openShopEvent.OnRaised -= OnOpenShop;
            notEnoughGoldEvent.OnRaised -= OnNotEnoughGold;
        }

        // ─────────────────────────────────────────────────────────────
        // Backdrop click — close if clicking outside the panel

        private void OnBackdropClick(ClickEvent evt)
        {
            if (_panel != null && !_panel.worldBound.Contains(evt.position))
                CloseShop();
        }

        // ─────────────────────────────────────────────────────────────
        // Tab switching

        private void SwitchTab(bool goldTab)
        {
            _isGoldTabActive = goldTab;

            if (_tabGoldContent != null)
                _tabGoldContent.style.display = goldTab ? DisplayStyle.Flex : DisplayStyle.None;

            if (_tabCosmeticContent != null)
                _tabCosmeticContent.style.display = goldTab ? DisplayStyle.None : DisplayStyle.Flex;

            // Active tab styling
            RefreshTabStyles();
        }

        private void RefreshTabStyles()
        {
            if (_tabGold != null)
            {
                _tabGold.EnableInClassList("shop-tab--active", _isGoldTabActive);
            }
            if (_tabCosmetic != null)
            {
                _tabCosmetic.EnableInClassList("shop-tab--active", !_isGoldTabActive);
            }

            if (_tabGoldLabel != null)
                _tabGoldLabel.style.color = _isGoldTabActive
                    ? new StyleColor(new Color(1f, 220f / 255f, 100f / 255f))
                    : new StyleColor(new Color(0.7f, 0.7f, 0.7f));

            if (_tabCosmeticLabel != null)
                _tabCosmeticLabel.style.color = !_isGoldTabActive
                    ? new StyleColor(new Color(200f / 255f, 180f / 255f, 1f))
                    : new StyleColor(new Color(0.7f, 0.7f, 0.7f));
        }

        // ─────────────────────────────────────────────────────────────
        // Not-enough-gold message

        private void SetupNotEnoughGoldLabel()
        {
            if (_messageArea == null)
                return;

            _notEnoughGoldLabel = new Label();
            _notEnoughGoldLabel.AddToClassList("shop-not-enough-gold-message");
            _notEnoughGoldLabel.style.display = DisplayStyle.None;
            _messageArea.Add(_notEnoughGoldLabel);
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

        // ─────────────────────────────────────────────────────────────
        // Open / Close

        private void OnOpenShop(string shopId)
        {
            bool isCurrentlyVisible = _shopRoot.style.display == DisplayStyle.Flex;

            if (isCurrentlyVisible)
            {
                CloseShop();
            }
            else
            {
                // Reset to gold tab on open
                _isGoldTabActive = true;
                RefreshTabStyles();
                SwitchTab(true);

                PopulateShop(shopId);
                OpenShop();
            }
        }

        private void OpenShop()
        {
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);

            _shopRoot.style.display = DisplayStyle.Flex;
            shopToggleEvent?.Raise(true);
            _animationCoroutine = StartCoroutine(AnimateOpen());
        }

        private void CloseShop()
        {
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);

            _animationCoroutine = StartCoroutine(AnimateClose());
        }

        // ─────────────────────────────────────────────────────────────
        // Animations (opacity + scale)

        private IEnumerator AnimateOpen()
        {
            if (_panel == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < AnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / AnimationDuration);
                float eased = EaseOutBack(t);

                _panel.style.opacity = Mathf.Lerp(0f, 1f, t);
                float scale = Mathf.Lerp(0.85f, 1f, eased);
                _panel.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));

                yield return null;
            }

            _panel.style.opacity = 1f;
            _panel.style.scale = new StyleScale(new Scale(Vector3.one));
            _animationCoroutine = null;
        }

        private IEnumerator AnimateClose()
        {
            if (_panel == null)
            {
                SetVisible(false, instant: true);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < AnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / AnimationDuration);

                _panel.style.opacity = Mathf.Lerp(1f, 0f, t);
                float scale = Mathf.Lerp(1f, 0.85f, t);
                _panel.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));

                yield return null;
            }

            SetVisible(false, instant: true);
            shopToggleEvent?.Raise(false);
            _animationCoroutine = null;
        }

        // Ease out back — slight overshoot for a bouncy feel on open
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private void SetVisible(bool visible, bool instant = false)
        {
            if (_shopRoot == null)
                return;
            _shopRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (instant && _panel != null)
            {
                _panel.style.opacity = visible ? 1f : 0f;
                float s = visible ? 1f : 0.85f;
                _panel.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Shop population (gold items only — cosmetics stubbed)

        private void PopulateShop(string shopId)
        {
            if (_shopItemsContainer == null)
                return;

            _shopItemsContainer.Clear();

            if (!ClientShopRegistry.TryGetShop(shopId, out ShopData shopData))
            {
                logger.Log(
                    $"[ShopUIController] Shop '{shopId}' not found.",
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
                var row = CreateProductRow(product);
                _shopItemsContainer.Add(row);
            }

            // Cosmetic items container is intentionally left empty for now.
            // TODO: populate _cosmeticItemsContainer when gem currency is implemented.

            logger.Log(
                $"[ShopUIController] Populated shop '{shopId}' with {products.Count} gold products.",
                this
            );
        }

        private VisualElement CreateProductRow(ProductData product)
        {
            var row = new VisualElement();
            row.AddToClassList("shop-item");

            // Icon
            var icon = new VisualElement();
            icon.AddToClassList("shop-item-icon");
            SlotItemLoader.LoadItem(icon, product.productId, itemAssetsService);

            // Name
            var nameLabel = new Label(product.productId);
            nameLabel.AddToClassList("shop-item-name");
            var itemData = ClientItemsRegistry.GetItemById(product.productId);
            if (itemData != null)
                nameLabel.text = itemData.name;

            // Price
            var priceLabel = new Label($"{product.price} {product.currency}");
            priceLabel.AddToClassList("shop-item-price");

            // Amount field
            var amountField = new IntegerField();
            amountField.AddToClassList("shop-item-amount");
            amountField.value = 1;
            amountField.RegisterCallback<ChangeEvent<int>>(evt =>
            {
                if (evt.newValue < 1)
                    amountField.SetValueWithoutNotify(1);
            });

            // Buy button
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

            // Tooltip
            icon.RegisterCallback<PointerEnterEvent>(_ =>
                _itemStatsTooltip?.ShowTooltip(capturedId, icon)
            );
            icon.RegisterCallback<PointerLeaveEvent>(_ => _itemStatsTooltip?.HideTooltip());

            return row;
        }
    }
}
