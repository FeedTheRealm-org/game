using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Gold
{
    [CreateAssetMenu(menuName = "Events/Client/Gold/PurchaseRequestEvent")]
    public class PurchaseRequestEvent
        : EventChannelSO<(string shopId, string productId, int amount)> { }
}
