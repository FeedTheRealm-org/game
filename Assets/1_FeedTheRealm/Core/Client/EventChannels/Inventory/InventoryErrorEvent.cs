using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Inventory
{
    [CreateAssetMenu(
        fileName = "InventoryErrorEvent",
        menuName = "Events/Client/Inventory/InventoryErrorEvent"
    )]
    public class InventoryErrorEvent : EventChannelSO<InventoryErrorType> { }
}
