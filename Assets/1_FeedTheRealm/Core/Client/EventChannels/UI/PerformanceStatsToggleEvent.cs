using System;
using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    /// <summary>
    /// Event channel for toggling the Performance Stats panel.
    /// Subscribe to OnRaised to show/hide the performance statistics.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PerformanceStatsToggleEvent",
        menuName = "Events/Client/UI/Performance Stats Toggle Event"
    )]
    public class PerformanceStatsToggleEvent : EventChannelSO<bool> { }
}
