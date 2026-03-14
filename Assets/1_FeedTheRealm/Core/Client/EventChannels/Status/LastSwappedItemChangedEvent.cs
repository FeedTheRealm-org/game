using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Status
{
    [CreateAssetMenu(menuName = "Events/Client/Status/LastSwappedItemChangedEvent")]
    public class LastSwappedItemChangedEvent : EventChannelSO<(int, int)> { }
}
