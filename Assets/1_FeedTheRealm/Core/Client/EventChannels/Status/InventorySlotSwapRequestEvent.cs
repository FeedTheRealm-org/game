using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Status
{
    [CreateAssetMenu(menuName = "Events/Client/Status/InventorySlotSwapRequest")]
    public class InventorySlotSwapRequestEvent : EventChannelSO<(int, int)> { }
}
