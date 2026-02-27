using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(menuName = "Events/Server/Ticks/GameTick")]
    public class GameTickEvent : EventChannelSO<float> { }
}
