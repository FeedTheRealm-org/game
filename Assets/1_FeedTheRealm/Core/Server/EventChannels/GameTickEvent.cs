using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(menuName = "Events/NetworkEvents/GameTick")]
    public class GameTickEvent : EventChannelSO<float> { }
}
