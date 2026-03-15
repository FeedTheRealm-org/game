using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Inventory
{
    [CreateAssetMenu(menuName = "Events/Client/Inventory/SlotEquipRequestEvent")]
    public class SlotEquipRequestEvent : EventChannelSO<(int, int)> { }
}
