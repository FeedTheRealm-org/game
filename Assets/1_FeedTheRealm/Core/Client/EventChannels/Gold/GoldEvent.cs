using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Gold
{
    [CreateAssetMenu(menuName = "Events/Client/Gold/GoldChangedEvent")]
    public class GoldChangedEvent : EventChannelSO<int> { }
}
