using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Portal
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Portal Toggle")]
    public class PortalToggleEvent : EventChannelSO<bool> { }
}
