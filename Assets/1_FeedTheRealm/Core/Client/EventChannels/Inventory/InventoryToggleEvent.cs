using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Inventory
{
    [CreateAssetMenu(menuName = "Events/Client/Inventory/InventoryToggle")]
    public class InventoryToggleEvent : EventChannelSO<bool> { }
}
