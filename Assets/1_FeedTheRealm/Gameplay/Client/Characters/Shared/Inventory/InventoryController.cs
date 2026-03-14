using FTR.Core.Client.EventChannels.Status;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;
using UnityEngine;
using VContainer;

public class InventoryController : MonoBehaviour
{
    [Inject]
    private InventorySlotSwapRequestEvent swapRequestEvent;

    private NetworkAdapter networkAdapter;
    private bool isInitialized = false;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        isInitialized = true;
        this.networkAdapter = networkAdapter;

        if (swapRequestEvent != null)
            swapRequestEvent.OnRaised += OnSwapRequest;
    }

    private void OnDestroy()
    {
        if (swapRequestEvent != null)
            swapRequestEvent.OnRaised -= OnSwapRequest;
    }

    private void OnSwapRequest((int sourceSlot, int targetSlot) data)
    {
        if (!isInitialized)
            return;

        Debug.Log(
            $"InventoryController sending MoveItem command from {data.sourceSlot} to {data.targetSlot}"
        );

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.MoveItem,
            Id = string.Empty,
            content = new MoveItemCommandContent
            {
                SourcePosition = data.sourceSlot,
                TargetPosition = data.targetSlot,
            }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }
}
