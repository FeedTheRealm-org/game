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
    void OnCancelInteract(IEventCollectable ec);
    void OnDialogNext(IEventCollectable ec);
    void OnEquipItem(IEventCollectable ec, int slotIndex);
    void OnDropItem(IEventCollectable ec, StorageType type, int slotIndex, string itemId);
    void OnPurchase(IEventCollectable ec, uint netId, string productId, int amount);
    void OnQuestAccepted(IEventCollectable ec, string questId);
    void OnQuestDecided(IEventCollectable ec);

    void OnPickUp(IEventCollectable ec, string itemId, int goldAmount, Action<bool> onComplete);
    void OnMoveItem(
        IEventCollectable ec,
        StorageType sourceType,
        int sourceSlot,
        StorageType targetType,
        int targetSlot
    );
    void OnSetUserId(IEventCollectable ec, string userId);
}
