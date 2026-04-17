using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(
        fileName = "ConsumeItemEvent",
        menuName = "Events/Server/Inventory/ConsumeItemEvent"
    )]
    public class ConsumeItemEvent : EventChannelSO<(uint playerNetId, string itemId)> { }
}
