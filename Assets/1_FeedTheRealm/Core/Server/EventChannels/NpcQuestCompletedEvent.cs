using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(
        fileName = "NpcQuestCompletedEvent",
        menuName = "Events/Server/Quests/NpcQuestCompletedEvent"
    )]
    public class NpcQuestCompletedEvent
        : EventChannelSO<(uint playerNetId, string questId, string npcId)> { }
}
