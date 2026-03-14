using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Inventory
{
    [CreateAssetMenu(menuName = "Events/Client/Inventory/SlotUnequipRequestEvent")]
    public class SlotUnequipRequestEvent : EventChannelSO<(int, int)> { }
}
