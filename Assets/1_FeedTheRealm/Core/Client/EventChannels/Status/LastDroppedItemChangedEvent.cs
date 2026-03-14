using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Status
{
    [CreateAssetMenu(menuName = "Events/Client/Status/LastDroppedItemChangedEvent")]
    public class LastDroppedItemChangedEvent : EventChannelSO<(string, int)> { }
}
