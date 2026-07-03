using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Inventory
{
    [CreateAssetMenu(
        fileName = "CooldownStartedEvent",
        menuName = "Events/Client/Inventory/CooldownStartedEvent"
    )]
    public class CooldownStartedEvent : EventChannelSO<(string itemId, float duration)> { }
}
