using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(
        fileName = "QuestRewardGoldEvent",
        menuName = "Events/Server/Quests/QuestRewardGoldEvent"
    )]
    public class QuestRewardGoldEvent : EventChannelSO<(uint playerNetId, int goldAmount)> { }
}
