using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Quests;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    [CreateAssetMenu(menuName = "Events/Quests/Quest Completed")]
    public class QuestCompletedEvent : EventChannelSO<(QuestData Quest, string EffectiveId)> { }
}
