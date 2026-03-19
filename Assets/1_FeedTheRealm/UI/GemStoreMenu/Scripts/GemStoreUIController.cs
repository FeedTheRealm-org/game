using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GemStoreController : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset packCardTemplate;

    [SerializeField]
    private API.PaymentService paymentService;

    [SerializeField]
    private Session.Session session;

    private UIDocument uiDocument;
    private PaymentCallbackServer callbackServer;

    private VisualElement packList;
    private Label statusLabel;
    private Label balanceLabel;
    private Button backButton;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        packList = root.Q<VisualElement>("PackList");
        statusLabel = root.Q<Label>("StatusLabel");
        balanceLabel = root.Q<Label>("GemBalanceLabel");
        backButton = root.Q<Button>("BackButton");

        backButton.clicked += OnBackClicked;

        LoadBalance();
        LoadPacks();
    }

    private void OnDisable()
    {
        backButton.clicked -= OnBackClicked;
        UnsubscribeCallbackServer();
        callbackServer.StopServer();
    }

    private async void LoadBalance()
    {
        (bool success, string message, API.GemBalanceResponse balance) =
            await paymentService.GetGemBalance(session.APIToken);

        if (success)
            balanceLabel.text = balance.gems.ToString();
    }

    private async void LoadPacks()
    {
        SetStatus("Loading packs...", visible: true);
        packList.Clear();

        (bool success, string message, List<API.GemPackResponse> packs) =
            await paymentService.GetAllGemPacks(session.APIToken);

        if (!success)
        {
            SetStatus(message, visible: true);
            return;
        }

        if (packs == null || packs.Count == 0)
        {
            SetStatus("No gem packs available.", visible: true);
            return;
        }

        SetStatus("", visible: false);

        int maxGems = 0;
        foreach (API.GemPackResponse p in packs)
            if (p.gems > maxGems)
                maxGems = p.gems;

        foreach (API.GemPackResponse pack in packs)
            BuildCard(pack, isFeatured: pack.gems == maxGems);
    }

    private void BuildCard(API.GemPackResponse pack, bool isFeatured)
    {
        TemplateContainer card = packCardTemplate.Instantiate();
        VisualElement root = card.Q<VisualElement>("PackCard");

        root.Q<Label>("PackGemIcon").text = "💎";
        root.Q<Label>("PackGemAmount").text = pack.gems.ToString();
        root.Q<Label>("PackPrice").text = $"${pack.price:F2}";

        if (isFeatured)
        {
            root.AddToClassList("pack-card--featured");
            root.Q<VisualElement>("CardGlow").AddToClassList("card-glow--featured");
            root.Q<Button>("BuyButton").AddToClassList("buy-button--featured");

            Label badge = new Label("BEST VALUE");
            badge.name = "FeaturedBadge";
            badge.AddToClassList("featured-badge");
            root.Insert(0, badge);
        }

        root.Q<Button>("BuyButton").clicked += () => OnBuyClicked(pack);

        packList.Add(card);
    }

    private void InitCallbackServer()
    {
        callbackServer = new PaymentCallbackServer();
    }

    private async void OnBuyClicked(API.GemPackResponse pack)
    {
        InitCallbackServer();

        (bool success, string message, API.CheckoutResponse checkout) =
            await paymentService.CreateCheckoutSession(
                pack.id,
                callbackServer.SuccessUrl,
                callbackServer.CancelUrl,
                session.APIToken
            );

        if (!success)
        {
            SetStatus(message, visible: true);
            return;
        }

        callbackServer.OnSuccessEvent += OnPaymentSuccess;
        callbackServer.OnCancelledEvent += OnPaymentCancelled;
        callbackServer.StartServer(
            new PaymentData
            {
                PackName = pack.name,
                GemAmount = pack.gems,
                Price = $"{pack.price:F2}",
                NewBalance = int.Parse(balanceLabel.text) + pack.gems,
            }
        );

        Application.OpenURL(checkout.checkout_url);
    }

    private void OnPaymentSuccess()
    {
        UnsubscribeCallbackServer();
        SetStatus("Payment successful! Gems added to your balance.", visible: true);
        LoadBalance();
    }

    private void OnPaymentCancelled()
    {
        UnsubscribeCallbackServer();
        SetStatus("Payment cancelled.", visible: true);
    }

    private void UnsubscribeCallbackServer()
    {
        callbackServer.OnSuccessEvent -= OnPaymentSuccess;
        callbackServer.OnCancelledEvent -= OnPaymentCancelled;
    }

    private void SetStatus(string text, bool visible)
    {
        statusLabel.text = text;
        statusLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnBackClicked()
    {
        // TODO: navigate back to previous screen
    }
}
