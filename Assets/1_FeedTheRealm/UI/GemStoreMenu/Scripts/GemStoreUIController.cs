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

    [SerializeField]
    private Logging.Logger logger;

    private UIDocument uiDocument;
    private PaymentCallbackServer callbackServer;

    private VisualElement packList;
    private Label balanceLabel;
    private Button backButton;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        packList = root.Q<VisualElement>("PackList");
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
        (bool success, string _, API.GemBalanceResponse balance) =
            await paymentService.GetGemBalance(session.APIToken);

        if (success)
            balanceLabel.text = balance.gems.ToString();
    }

    private async void LoadPacks()
    {
        packList.Clear();

        (bool success, string message, List<API.GemPackResponse> packs) =
            await paymentService.GetAllGemPacks(session.APIToken);

        if (!success)
        {
            SetStatus(message, isError: true);
            return;
        }

        int bestGemPack = 0;
        decimal bestRatioPack = decimal.MaxValue;
        foreach (API.GemPackResponse p in packs)
        {
            decimal currentRatioPack =
                decimal.Parse(p.price, System.Globalization.CultureInfo.InvariantCulture) / p.gems;
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
        root.Q<Label>("PackPrice").text =
            $"${decimal.Parse(pack.price, System.Globalization.CultureInfo.InvariantCulture)}";

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
        callbackServer = gameObject.GetComponent<PaymentCallbackServer>();
        if (callbackServer == null)
            logger.Log(
                "PaymentCallbackServer component is missing on GemStoreController GameObject.",
                this,
                Logging.LogType.Error
            );
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
            SetStatus(message, isError: true);
            return;
        }

        callbackServer.OnSuccessEvent += OnPaymentSuccess;
        callbackServer.OnCancelledEvent += OnPaymentCancelled;

        int currentBalance = 0;
        if (!int.TryParse(balanceLabel.text, out currentBalance))
            currentBalance = 0;

        _ = callbackServer
            .StartServer(
                new PaymentData
                {
                    PackName = pack.name,
                    GemAmount = pack.gems,
                    Price = $"{pack.price:F2}",
                    NewBalance = currentBalance + pack.gems,
                }
            )
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                    logger.Log(
                        $"Payment server error: {task.Exception?.Message}",
                        this,
                        Logging.LogType.Error
                    );
            });

        Application.OpenURL(checkout.checkout_url);
    }

    private void OnPaymentSuccess()
    {
        UnsubscribeCallbackServer();
        SetStatus("Payment successful! Gems added to your balance.");
        LoadBalance();
    }

    private void OnPaymentCancelled()
    {
        UnsubscribeCallbackServer();
        SetStatus("Payment cancelled.", isError: true);
    }

    private void UnsubscribeCallbackServer()
    {
        callbackServer.OnSuccessEvent -= OnPaymentSuccess;
        callbackServer.OnCancelledEvent -= OnPaymentCancelled;
    }

    private void SetStatus(string text, bool isError = false)
    {
        if (!isError)
            ToastNotification.Show($"{text}", "success", Color.green);
        else
            ToastNotification.Show($"{text}", "error", Color.red);
    }

    private void OnBackClicked()
    {
        logger.Log("Back Button Clicked", this);
        gameObject.SetActive(false);
    }
}
