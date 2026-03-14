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

    [Inject]
    private InventorySlotDropRequestEvent dropRequestEvent;

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

    private void OnDropRequest(int slot)
    {
        if (!isInitialized)
            return;

        Debug.Log($"InventoryController sending Drop command for slot {slot}");

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.DropItem,
            Id = string.Empty,
            content = new DropItemCommandContent { Position = slot }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }
}
