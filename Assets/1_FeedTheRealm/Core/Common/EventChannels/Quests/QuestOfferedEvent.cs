using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Quests;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    [CreateAssetMenu(menuName = "Events/Quests/Quest Offered")]
    public class QuestOfferedEvent : EventChannelSO<QuestData> { }
}
