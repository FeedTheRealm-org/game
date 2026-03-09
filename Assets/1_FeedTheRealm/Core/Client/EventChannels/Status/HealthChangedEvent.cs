using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Status
{
    [CreateAssetMenu(menuName = "Events/Client/Status/HealthChanged")]
    public class HealthChangedEvent : EventChannelSO<HealthChangedData> { }
}
