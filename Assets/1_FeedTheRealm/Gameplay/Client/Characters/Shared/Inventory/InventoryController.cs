using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;
using UnityEngine;
using VContainer;

public class InventoryController : MonoBehaviour
{
    [Inject]
    private SlotSwapRequestEvent swapRequestEvent;

    [Inject]
    private SlotDropRequestEvent dropRequestEvent;

    private NetworkAdapter networkAdapter;
    private bool isInitialized = false;

    public void Initialize(NetworkAdapter networkAdapter)
    {
        isInitialized = true;
        this.networkAdapter = networkAdapter;

        if (swapRequestEvent != null)
            swapRequestEvent.OnRaised += OnSwapRequest;

        if (dropRequestEvent != null)
            dropRequestEvent.OnRaised += OnDropRequest;
    }

    private void OnDestroy()
    {
        if (swapRequestEvent != null)
            swapRequestEvent.OnRaised -= OnSwapRequest;

        if (dropRequestEvent != null)
            dropRequestEvent.OnRaised -= OnDropRequest;
    }

    private void OnSwapRequest(
        (StorageType sourceType, int sourceSlot, StorageType targetType, int targetSlot) data
    )
    {
        if (!isInitialized)
            return;

        Debug.Log(
            $"InventoryController sending MoveItem: "
                + $"{data.sourceType}[{data.sourceSlot}] -> {data.targetType}[{data.targetSlot}]"
        );

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.MoveItem,
            Id = string.Empty,
            content = new MoveItemCommandContent
            {
                SourceType = data.sourceType,
                SourcePosition = data.sourceSlot,
                TargetType = data.targetType,
                TargetPosition = data.targetSlot,
            }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }

    private void OnDropRequest((StorageType storageType, int slot) data)
    {
        if (!isInitialized)
            return;

        Debug.Log($"InventoryController sending DropItem: {data.storageType}[{data.slot}]");

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.DropItem,
            Id = string.Empty,
            content = new DropItemCommandContent
            {
                Type = data.storageType,
                Position = data.slot,
            }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }
}
