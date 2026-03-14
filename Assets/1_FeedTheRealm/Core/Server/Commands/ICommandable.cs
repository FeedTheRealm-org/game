using System;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using UnityEngine;

namespace FTR.Core.Server.Commands;

public interface ICommandable
{
    void OnMove(IEventCollectable ec, Vector3 direction);
    void OnDash(IEventCollectable ec, Vector3 direction);
    void OnUse(IEventCollectable ec);
    void OnInteract(IEventCollectable ec);
    void OnEquipItem(IEventCollectable ec, int sourceSlot, int targetSlot, string itemId);
    void OnUnequipItem(IEventCollectable ec, int sourceSlot, int targetSlot, string itemId);
    void OnDropItem(IEventCollectable ec, StorageType type, int slotIndex, string itemId);
    void OnPurchase(IEventCollectable ec);
    void OnQuestAccepted(IEventCollectable ec);
    void OnPickUp(IEventCollectable ec, string itemId, Action<bool> onComplete);
    void OnMoveItem(IEventCollectable ec, StorageType type, int sourceSlot, int targetSlot);
}
