using FTR.Core.Client.Quests;
using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels
{
    [CreateAssetMenu(menuName = "Events/Quests/Quest Progress")]
    public class QuestProgressEvent : EventChannelSO<QuestProgressData> { }
}
