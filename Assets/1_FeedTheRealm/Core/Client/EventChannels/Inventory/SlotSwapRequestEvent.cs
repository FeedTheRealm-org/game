using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Inventory
{
    [CreateAssetMenu(menuName = "Events/Client/Inventory/SlotSwapRequest")]
    public class SlotSwapRequestEvent : EventChannelSO<(StorageType, int, int)> { }
}
