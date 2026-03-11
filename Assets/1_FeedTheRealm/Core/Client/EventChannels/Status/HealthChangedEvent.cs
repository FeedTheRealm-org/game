using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Systems.Status;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Status
{
    [CreateAssetMenu(menuName = "Events/Client/Status/HealthChanged")]
    public class HealthChangedEvent : EventChannelSO<HealthChangedData> { }
}
