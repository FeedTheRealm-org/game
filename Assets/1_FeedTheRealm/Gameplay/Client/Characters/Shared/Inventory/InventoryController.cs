using System.Data.SqlTypes;
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

    [Inject]
    private SlotEquipRequestEvent equipRequestEvent;

    [Inject]
    private SlotUnequipRequestEvent unequipRequestEvent;

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

        if (equipRequestEvent != null)
            equipRequestEvent.OnRaised += OnEquipRequest;

        if (unequipRequestEvent != null)
            unequipRequestEvent.OnRaised += OnUnEquipRequest;
    }

    private void OnDestroy()
    {
        if (swapRequestEvent != null)
            swapRequestEvent.OnRaised -= OnSwapRequest;

        if (dropRequestEvent != null)
            dropRequestEvent.OnRaised -= OnDropRequest;

        if (equipRequestEvent != null)
            equipRequestEvent.OnRaised -= OnEquipRequest;

        if (unequipRequestEvent != null)
            unequipRequestEvent.OnRaised -= OnUnEquipRequest;
    }

    private void OnSwapRequest((StorageType type, int sourceSlot, int targetSlot) data)
    {
        if (!isInitialized || data.type != StorageType.Inventory)
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
                Type = StorageType.Inventory,
                SourcePosition = data.sourceSlot,
                TargetPosition = data.targetSlot,
            }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }

    private void OnDropRequest((StorageType type, int slot) data)
    {
        if (!isInitialized || data.type != StorageType.Inventory)
            return;

        Debug.Log($"InventoryController sending Drop command for slot {data.slot}");

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.DropItem,
            Id = string.Empty,
            content = new DropItemCommandContent
            {
                Type = StorageType.Inventory,
                Position = data.slot,
            }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }

    private void OnEquipRequest((int sourceSlot, int targetSlot) data)
    {
        if (!isInitialized)
            return;

        Debug.Log(
            $"InventoryController sending EquipItem command for slot {data.sourceSlot} to {data.targetSlot}"
        );

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.EquipItem,
            Id = string.Empty,
            content = new MoveItemCommandContent
            {
                Type = StorageType.Inventory,
                SourcePosition = data.sourceSlot,
                TargetPosition = data.targetSlot,
            }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }

    private void OnUnEquipRequest((int sourceSlot, int targetSlot) data)
    {
        if (!isInitialized)
            return;

        Debug.Log(
            $"InventoryController sending UnequipItem command for slot {data.sourceSlot} to {data.targetSlot}"
        );

        TransactionCommandDTO command = new()
        {
            Type = TransactionType.UnequipItem,
            Id = string.Empty,
            content = new MoveItemCommandContent
            {
                Type = StorageType.Inventory,
                SourcePosition = data.sourceSlot,
                TargetPosition = data.targetSlot,
            }.ToByteArray(),
        };

        networkAdapter.DispatchTransaction(command);
    }
}
