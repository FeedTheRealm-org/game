using System;
using System.Threading.Tasks;
using Game.Core.Events;
using Mono.Cecil;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ShopMenuController : MonoBehaviour
{
    UIDocument uiDocument;
    VisualElement root;
    VisualElement panel;

    [SerializeField]
    private ShopInteractedEvent shopInteractedEvent;

    [SerializeField]
    private ShopOnCloseEvent shopOnCloseEvent;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private ShopItemsSO shopItemsSO;

    [SerializeField]
    private API.ItemAssetsService itemAssetsService;

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogWarning("ShopMenuController requires a UIDocument on the same GameObject.");
            return;
        }

        root = uiDocument.rootVisualElement;
        panel = root.Q<VisualElement>("Panel");

        shopInteractedEvent.OnRaised += () => ToggleShopMenu(true);

        PopulateShopItems();
        ToggleShopMenu(false);

        var closeBtn = root.Q<Button>("CloseButton");
        if (closeBtn != null)
            closeBtn.clicked += () => OnCloseMenu();
    }

    private void ToggleShopMenu(bool show)
    {
        logger.Log("Toggling shop menu: " + show, this);
        logger.Log($"Before Root style display: {root.style.display}", this);
        root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        logger.Log($"After Root style display: {root.style.display}", this);
        UnityEngine.Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        UnityEngine.Cursor.visible = show;
    }

    private void OnCloseMenu()
    {
        ToggleShopMenu(false);
        shopOnCloseEvent.Raise();
    }

    private async Task AddProductToUI(Models.ProductData product)
    {
        var item = new VisualElement();
        item.name = "ShopItem";
        item.AddToClassList("shop-slot");

        var img = new VisualElement();
        Texture2D texture = await itemAssetsService.DownloadItemSpriteAsync(product.itemData.id);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        img.style.backgroundImage = new StyleBackground(sprite);
        img.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        img.style.width = new Length(100, LengthUnit.Percent);
        img.style.height = new Length(100, LengthUnit.Percent);
        img.style.position = Position.Relative;
        img.style.alignItems = Align.Center;
        img.style.justifyContent = Justify.Center;

        var priceLabel = new Label($"Price: {product.price}");
        priceLabel.AddToClassList("shop-item-price");

        var itemContainer = new VisualElement();
        itemContainer.AddToClassList("shop-item-container");

        item.Add(img);
        itemContainer.Add(item);
        itemContainer.Add(priceLabel);

        panel.Add(itemContainer);
    }

    private async void PopulateShopItems()
    {
        foreach (var product in shopItemsSO.GetShopData().products)
        {
            logger.Log($"Adding item to shop UI: {product.itemData.name}", this);
            await AddProductToUI(product);
        }
    }
}
