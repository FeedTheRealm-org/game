using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(
        fileName = "PlayerHealEvent",
        menuName = "Events/Server/Stats/PlayerHealEvent"
    )]
    public class PlayerHealEvent : EventChannelSO<(uint playerNetId, float amount)> { }
}
