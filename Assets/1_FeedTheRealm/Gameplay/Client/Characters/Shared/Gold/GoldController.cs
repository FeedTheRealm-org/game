using FTR.Core.Client.EventChannels.Gold;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;
using UnityEngine;
using VContainer;

public class GoldController : MonoBehaviour
{
    [Inject]
    private PurchaseRequestEvent purchaseRequestEvent;

    private NetworkAdapter networkAdapter;
    private bool isInitialized = false;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        isInitialized = true;
        this.networkAdapter = networkAdapter;

        if (purchaseRequestEvent != null)
            purchaseRequestEvent.OnRaised += OnPurchaseRequest;
    }

    private void OnDestroy()
    {
        if (purchaseRequestEvent != null)
            purchaseRequestEvent.OnRaised -= OnPurchaseRequest;
    }

    private void OnPurchaseRequest((string productId, int amount) data)
    {
        if (!isInitialized)
            return;

        Debug.Log($"GoldController sending Purchase: {data.productId} x{data.amount}");

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.Purchase,
            Id = string.Empty,
            content = new PurchaseCommandContent
            {
                ProductId = data.productId,
                Amount = data.amount,
            }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }
}
