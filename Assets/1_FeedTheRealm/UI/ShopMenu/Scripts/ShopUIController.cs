using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enums;
using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Gold;
using FTR.Core.Client.EventChannels.Input;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Client.EventChannels.Shop;
using FTR.Core.Client.Managers;
using FTR.Core.Common.Protocol.RpcMessages;
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
        private API.AssetsService assetsService;

        [SerializeField]
        private API.PaymentService paymentService;

        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private ItemStatsTooltip itemStatsTooltipPrefab;

        [Inject]
        private OpenShopEvent openShopEvent;

        [Inject]
        private ShopToggleEvent shopToggleEvent;

        [Inject]
        private PurchaseRequestEvent purchaseRequestEvent;

        [Inject]
        private InventoryErrorEvent inventoryErrorEvent;

        [Inject]
        private MenuManager menuManager;

        [Inject]
        private BackEvent backEvent;

        [Inject]
        private ISoundPlayer soundPlayer;

        private VisualElement _shopRoot;
        private VisualElement _panel;

        private VisualElement _closeButton;
        private VisualElement _gemBalanceContainer;
        private Label _gemBalanceLabel;

        private VisualElement _tabGold;
        private VisualElement _tabCosmetic;
        private Label _tabGoldLabel;
        private Label _tabCosmeticLabel;
        private VisualElement _tabGoldContent;
        private VisualElement _tabCosmeticContent;

        private VisualElement _shopItemsContainer;
        private VisualElement _cosmeticItemsContainer;

        private VisualElement _messageArea;
        private Label _feedbackLabel;

        private bool _isGoldTabActive = true;
        private int _currentGemBalance = -1;
        private string _currentShopId;

        private ItemStatsTooltip _itemStatsTooltip;
        private Coroutine _hideMessageCoroutine;
        private Coroutine _animationCoroutine;
        private Coroutine _fetchGemBalanceCoroutine;

        private const float AnimationDuration = 0.25f;

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

            _closeButton = _shopRoot.Q<VisualElement>("CloseButton");
            _gemBalanceContainer = _shopRoot.Q<VisualElement>("GemBalanceContainer");
            _gemBalanceLabel = _shopRoot.Q<Label>("GemBalanceLabel");

            _closeButton?.RegisterCallback<ClickEvent>(_ => CloseShop());

            _tabGold = _shopRoot.Q<VisualElement>("TabGold");
            _tabCosmetic = _shopRoot.Q<VisualElement>("TabCosmetic");
            _tabGoldLabel = _shopRoot.Q<Label>("TabGoldLabel");
            _tabCosmeticLabel = _shopRoot.Q<Label>("TabCosmeticLabel");
            _tabGoldContent = _shopRoot.Q<VisualElement>("TabGoldContent");
            _tabCosmeticContent = _shopRoot.Q<VisualElement>("TabCosmeticContent");

            _tabGold?.RegisterCallback<ClickEvent>(_ => SwitchTab(true));
            _tabCosmetic?.RegisterCallback<ClickEvent>(_ => SwitchTab(false));

            _shopItemsContainer = _shopRoot.Q<VisualElement>("ShopItemsContainer");
            _cosmeticItemsContainer = _shopRoot.Q<VisualElement>("CosmeticItemsContainer");

            _messageArea = _shopRoot.Q<VisualElement>("MessageArea");
            SetupFeedbackLabel();

            SetVisible(false, instant: true);

            openShopEvent.OnRaised += OnOpenShop;
            inventoryErrorEvent.OnRaised += OnInventoryError;
            backEvent.OnRaised += CloseShop;
        }

        private void OnDisable()
        {
            openShopEvent.OnRaised -= OnOpenShop;
            inventoryErrorEvent.OnRaised -= OnInventoryError;
            backEvent.OnRaised -= CloseShop;
        }

        private void SwitchTab(bool goldTab)
        {
            _isGoldTabActive = goldTab;

            if (_tabGoldContent != null)
                _tabGoldContent.style.display = goldTab ? DisplayStyle.Flex : DisplayStyle.None;

            if (_tabCosmeticContent != null)
                _tabCosmeticContent.style.display = goldTab ? DisplayStyle.None : DisplayStyle.Flex;

            if (_gemBalanceContainer != null)
                _gemBalanceContainer.style.display = goldTab
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;

            if (!goldTab && _currentGemBalance < 0 && _fetchGemBalanceCoroutine == null)
                _fetchGemBalanceCoroutine = StartCoroutine(FetchGemBalance());

            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);

            RefreshTabStyles();
        }

        private void RefreshTabStyles()
        {
            _tabGold?.EnableInClassList("shop-tab--active", _isGoldTabActive);
            _tabCosmetic?.EnableInClassList("shop-tab--active", !_isGoldTabActive);

            if (_tabGoldLabel != null)
                _tabGoldLabel.style.color = _isGoldTabActive
                    ? new StyleColor(new Color(1f, 220f / 255f, 100f / 255f))
                    : new StyleColor(new Color(0.7f, 0.7f, 0.7f));

            if (_tabCosmeticLabel != null)
                _tabCosmeticLabel.style.color = !_isGoldTabActive
                    ? new StyleColor(new Color(200f / 255f, 180f / 255f, 1f))
                    : new StyleColor(new Color(0.7f, 0.7f, 0.7f));
        }

        private IEnumerator FetchGemBalance()
        {
            if (_gemBalanceLabel != null)
                _gemBalanceLabel.text = "…";

            var task = paymentService.GetGemBalance();
            yield return new WaitUntil(() => task.IsCompleted);

            var (success, _, balance) = task.Result;
            if (success && balance != null)
            {
                _currentGemBalance = balance.gems;
                UpdateGemBalanceLabel();
            }
            else
            {
                if (_gemBalanceLabel != null)
                    _gemBalanceLabel.text = "?";
            }

            _fetchGemBalanceCoroutine = null;
        }

        private void UpdateGemBalanceLabel()
        {
            if (_gemBalanceLabel != null)
                _gemBalanceLabel.text =
                    _currentGemBalance >= 0 ? _currentGemBalance.ToString("N0") : "—";
        }

        private void SetupFeedbackLabel()
        {
            if (_messageArea == null)
                return;
            _feedbackLabel = new Label();
            _feedbackLabel.AddToClassList("shop-not-enough-gold-message");
            _feedbackLabel.style.display = DisplayStyle.None;
            _messageArea.Add(_feedbackLabel);
        }

        private void ShowFeedback(string message)
        {
            if (_feedbackLabel == null)
                return;
            _feedbackLabel.text = message;
            _feedbackLabel.style.display = DisplayStyle.Flex;

            if (_hideMessageCoroutine != null)
                StopCoroutine(_hideMessageCoroutine);
            _hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay(3f));
        }

        private void OnInventoryError(InventoryErrorType errorType)
        {
            if (errorType == InventoryErrorType.NotEnoughGold)
            {
                ShowFeedback("Not enough gold to buy this item!");
            }
            else if (errorType == InventoryErrorType.NotEnoughSpace)
            {
                ShowFeedback("Not enough space in your inventory!");
            }
        }

        private IEnumerator HideMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_feedbackLabel != null)
                _feedbackLabel.style.display = DisplayStyle.None;
            _hideMessageCoroutine = null;
        }

        private void OnOpenShop(string shopId)
        {
            bool isVisible = _shopRoot.style.display == DisplayStyle.Flex;
            if (isVisible)
            {
                CloseShop();
                return;
            }

            _isGoldTabActive = true;
            _currentGemBalance = -1;
            RefreshTabStyles();
            SwitchTab(true);

            PopulateShop(shopId);
            OpenShop();
        }

        private void OpenShop()
        {
            if (!menuManager.CanOpenMenu(MenuType.Shop))
                return;

            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            _shopRoot.style.display = DisplayStyle.Flex;
            shopToggleEvent?.Raise(true);
            _animationCoroutine = StartCoroutine(AnimateOpen());
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.OpenUI);

            menuManager.ToggleMenu(MenuType.Shop, true);
        }

        private void CloseShop()
        {
            if (!_shopRoot.style.display.Equals(DisplayStyle.Flex))
                return;
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateClose());
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.CloseUI);

            menuManager.ToggleMenu(MenuType.Shop, false);
        }

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
                float s = Mathf.Lerp(0.85f, 1f, eased);
                _panel.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
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
                float s = Mathf.Lerp(1f, 0.85f, t);
                _panel.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
                yield return null;
            }
            SetVisible(false, instant: true);
            shopToggleEvent?.Raise(false);
            _animationCoroutine = null;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f,
                c3 = c1 + 1f;
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

        private void PopulateShop(string shopId)
        {
            _currentShopId = shopId;
            if (_shopItemsContainer == null || _cosmeticItemsContainer == null)
                return;

            _shopItemsContainer.Clear();
            _cosmeticItemsContainer.Clear();

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

            int goldCount = 0,
                gemCount = 0;

            foreach (var product in products)
            {
                if (product.currency == CurrencyType.Gold)
                {
                    _shopItemsContainer.Add(CreateGoldRow(product));
                    goldCount++;
                }
                else if (product.currency == CurrencyType.Gems)
                {
                    _cosmeticItemsContainer.Add(CreateCosmeticRow(product));
                    gemCount++;
                }
            }

            logger.Log(
                $"[ShopUIController] Shop '{shopId}': {goldCount} gold, {gemCount} gem products.",
                this
            );
        }

        private VisualElement CreateGoldRow(ProductData product)
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

            var priceLabel = new Label($"{product.price} 🪙");
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
                purchaseRequestEvent?.Raise((_currentShopId, capturedId, amount));
            });

            row.Add(icon);
            row.Add(nameLabel);
            row.Add(priceLabel);
            row.Add(amountField);
            row.Add(buyButton);

            icon.RegisterCallback<PointerEnterEvent>(_ =>
                _itemStatsTooltip?.ShowTooltip(capturedId, icon)
            );
            icon.RegisterCallback<PointerLeaveEvent>(_ => _itemStatsTooltip?.HideTooltip());

            return row;
        }

        private VisualElement CreateCosmeticRow(ProductData product)
        {
            var row = new VisualElement();
            row.AddToClassList("shop-item");

            var icon = new VisualElement();
            icon.AddToClassList("shop-item-icon");
            StartCoroutine(
                LoadCosmeticIcon(
                    icon,
                    ResolveCosmeticSpriteReference(product),
                    product.categoryName
                )
            );

            var nameLabel = new Label(
                !string.IsNullOrEmpty(product.displayName) ? product.displayName : product.productId
            );
            nameLabel.AddToClassList("shop-item-name");

            var categoryLabel = new Label(product.categoryName ?? "");
            categoryLabel.AddToClassList("shop-item-category");

            var namesColumn = new VisualElement();
            namesColumn.AddToClassList("shop-item-names-column");
            namesColumn.Add(nameLabel);
            if (!string.IsNullOrEmpty(product.categoryName))
                namesColumn.Add(categoryLabel);

            var priceLabel = new Label($"{product.price} 💎");
            priceLabel.AddToClassList("shop-item-price");

            var buyButton = new VisualElement();
            buyButton.AddToClassList("shop-buy-button");
            var buyLabel = new Label("Buy");
            buyLabel.AddToClassList("shop-buy-button-label");
            buyButton.Add(buyLabel);

            string purchaseId = ResolveCosmeticPurchaseProductId(product);
            string tooltipId = product.productId;
            buyButton.RegisterCallback<ClickEvent>(_ =>
            {
                StartCoroutine(ProcessGemPurchase(purchaseId, product.displayName));
            });

            row.Add(icon);
            row.Add(namesColumn);
            row.Add(priceLabel);
            row.Add(buyButton);

            icon.RegisterCallback<PointerEnterEvent>(_ =>
                _itemStatsTooltip?.ShowTooltip(tooltipId, icon)
            );
            icon.RegisterCallback<PointerLeaveEvent>(_ => _itemStatsTooltip?.HideTooltip());

            return row;
        }

        private string ResolveCosmeticPurchaseProductId(ProductData product)
        {
            if (
                product == null
                || string.IsNullOrEmpty(product.productId)
                || string.IsNullOrEmpty(product.categoryName)
            )
                return product?.productId;

            var cosmetics = ClientShopRegistry.CurrentWorldData?.cosmetics;
            if (cosmetics == null)
                return product.productId;

            var cosmetic = cosmetics.FirstOrDefault(c => c.id == product.productId);
            if (cosmetic == null)
                return product.productId;

            var urlId = cosmetic.GetUrlId(product.categoryName);
            return string.IsNullOrEmpty(urlId) ? product.productId : urlId;
        }

        private string ResolveCosmeticSpriteReference(ProductData product)
        {
            if (
                product == null
                || string.IsNullOrEmpty(product.productId)
                || string.IsNullOrEmpty(product.categoryName)
            )
                return product?.productId;

            var cosmetics = ClientShopRegistry.CurrentWorldData?.cosmetics;
            if (cosmetics == null)
                return product.productId;

            var cosmetic = cosmetics.FirstOrDefault(c => c.id == product.productId);
            if (cosmetic == null)
                return product.productId;

            var urlId = cosmetic.GetUrlId(product.categoryName);
            if (!string.IsNullOrEmpty(urlId))
                return urlId;

            var spritePath = cosmetic.GetSpritePath(product.categoryName);
            return string.IsNullOrEmpty(spritePath) ? product.productId : spritePath;
        }

        private IEnumerator LoadCosmeticIcon(
            VisualElement icon,
            string cosmeticId,
            string categoryName
        )
        {
            var task = assetsService.DownloadTexture2D(cosmeticId);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result == null)
                yield break;

            Sprite sprite;

            if (!string.IsNullOrEmpty(categoryName))
            {
                var cropped = CosmeticIconLoader.CreateCroppedSprite(task.Result, categoryName);
                if (cropped != null)
                {
                    sprite = cropped;
                }
                else
                {
                    sprite = CosmeticIconLoader.CreateFullSprite(task.Result);
                }
            }
            else
            {
                sprite = CosmeticIconLoader.CreateFullSprite(task.Result);
            }

            icon.style.backgroundImage = new StyleBackground(sprite);
            icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        }

        private IEnumerator ProcessGemPurchase(string cosmeticId, string displayName)
        {
            var task = paymentService.PurchaseWithGems(cosmeticId);
            yield return new WaitUntil(() => task.IsCompleted);

            var (success, message, updatedBalance) = task.Result;

            if (success)
            {
                if (updatedBalance != null)
                {
                    _currentGemBalance = updatedBalance.gems;
                    UpdateGemBalanceLabel();
                }

                soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.Purchase);

                string label = !string.IsNullOrEmpty(displayName) ? displayName : cosmeticId;
                ShowFeedback($"Purchased {label}!");
            }
            else
            {
                ShowFeedback(message);
            }
        }
    }
}
