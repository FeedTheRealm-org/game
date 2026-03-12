using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Status
{
    [CreateAssetMenu(menuName = "Events/Client/Status/LastItemChanged")]
    public class LastItemChangedEvent : EventChannelSO<(string, byte)> { }
}
