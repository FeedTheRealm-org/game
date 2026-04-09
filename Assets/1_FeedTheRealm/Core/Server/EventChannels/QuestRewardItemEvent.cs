using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(
        fileName = "QuestRewardItemEvent",
        menuName = "Events/Server/Quests/QuestRewardItemEvent"
    )]
    public class QuestRewardItemEvent : EventChannelSO<(uint playerNetId, string itemId)> { }
}
