using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(
        fileName = "ItemEquippedEvent",
        menuName = "Events/Server/Inventory/ItemEquippedEvent"
    )]
    public class ItemEquippedEvent
        : EventChannelSO<(uint playerNetId, string itemId, int slotIndex)> { }
}
