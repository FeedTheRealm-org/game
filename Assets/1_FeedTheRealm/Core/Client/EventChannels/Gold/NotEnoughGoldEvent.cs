using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Gold
{
    [CreateAssetMenu(menuName = "Events/Client/Gold/NotEnoughGold")]
    public class NotEnoughGoldEvent : EventChannelSO<(string productId, int amount)> { }
}
