using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Status
{
    [CreateAssetMenu(menuName = "Events/Client/Status/InventoryToggle")]
    public class InventoryToggleEvent : EventChannelSO<bool> { }
}
