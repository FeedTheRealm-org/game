using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;
using UnityEngine;
using VContainer;

public class FastSlotController : MonoBehaviour
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

    private void OnSwapRequest((StorageType type, int sourceSlot, int targetSlot) data)
    {
        if (!isInitialized || data.type != StorageType.FastSlot)
            return;

        Debug.Log(
            $"FastSlotController sending MoveItem command from {data.sourceSlot} to {data.targetSlot}"
        );

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.MoveItem,
            Id = string.Empty,
            content = new MoveItemCommandContent
            {
                Type = StorageType.FastSlot,
                SourcePosition = data.sourceSlot,
                TargetPosition = data.targetSlot,
            }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }

    private void OnDropRequest((StorageType type, int slot) data)
    {
        if (!isInitialized || data.type != StorageType.FastSlot)
            return;

        Debug.Log($"FastSlotController sending Drop command for slot {data.slot}");

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.DropItem,
            Id = string.Empty,
            content = new DropItemCommandContent
            {
                Type = StorageType.FastSlot,
                Position = data.slot,
            }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }
}
