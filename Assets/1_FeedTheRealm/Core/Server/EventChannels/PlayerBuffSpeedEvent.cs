using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(
        fileName = "PlayerBuffSpeedEvent",
        menuName = "Events/Server/Stats/PlayerBuffSpeedEvent"
    )]
    public class PlayerBuffSpeedEvent
        : EventChannelSO<(uint playerNetId, float speedBoost, float duration)> { }
}
