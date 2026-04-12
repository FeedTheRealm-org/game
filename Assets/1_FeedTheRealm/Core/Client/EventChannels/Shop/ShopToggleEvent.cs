using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Shop
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Shop Toggle")]
    public class ShopToggleEvent : EventChannelSO<bool> { }
}
