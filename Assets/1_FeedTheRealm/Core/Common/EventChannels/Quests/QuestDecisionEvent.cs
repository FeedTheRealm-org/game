using FTR.Core.Common.EventChannels;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    [CreateAssetMenu(menuName = "Events/Quests/Quest Decision")]
    public class QuestDecisionEvent : EventChannelSO<QuestDecisionData> { }
}
