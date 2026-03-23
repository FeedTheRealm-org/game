using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

    [SerializeField]
    private Logging.Logger logger;

    private UIDocument uiDocument;
    private PaymentCallbackServer callbackServer;

    private VisualElement packList;
    private Label statusLabel;
    private Label balanceLabel;
    private Button backButton;
    private CancellationTokenSource statusCts;

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

        int bestGemPack = 0;
        float bestRatioPack = float.MaxValue;
        foreach (API.GemPackResponse p in packs)
        {
            float currentRatioPack = (float)p.price / p.gems;
            if (currentRatioPack < bestRatioPack)
            {
                bestRatioPack = currentRatioPack;
                bestGemPack = p.gems;
            }
        }

        foreach (API.GemPackResponse pack in packs)
            BuildCard(pack, isFeatured: pack.gems == bestGemPack);
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
        statusCts?.Cancel();
        statusCts?.Dispose();
        statusCts = null;

        statusLabel.text = text;
        statusLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        statusCts = new CancellationTokenSource();
        Task.Run(() => HideStatusAfterDelayAsync(statusCts.Token), statusCts.Token);
    }

    private async Task HideStatusAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(5000, token);
            statusLabel.style.display = DisplayStyle.None;
        }
        catch (OperationCanceledException) { }
    }

    private void OnBackClicked()
    {
        logger.Log("Back Button Clicked", this);
        gameObject.SetActive(false);
    }
}
