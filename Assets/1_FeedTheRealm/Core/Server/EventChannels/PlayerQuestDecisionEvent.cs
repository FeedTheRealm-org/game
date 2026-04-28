using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(
        fileName = "PlayerQuestDecisionEvent",
        menuName = "Events/Server/Quests/PlayerQuestDecisionEvent"
    )]
    public class PlayerQuestDecisionEvent : EventChannelSO<(uint playerNetId, bool isAccepted)> { }
}
