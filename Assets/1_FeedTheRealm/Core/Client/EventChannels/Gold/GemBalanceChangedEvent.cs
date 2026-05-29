using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Gold
{
    [CreateAssetMenu(menuName = "Events/Client/Gold/GemBalanceChangedEvent")]
    public class GemBalanceChangedEvent : EventChannelSO<(int currentBalance, int delta)> { }
}
