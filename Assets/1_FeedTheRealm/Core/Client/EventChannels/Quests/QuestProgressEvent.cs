using FTR.Core.Client.Quests;
using FTR.Core.Common.Events;
using UnityEngine;

namespace FTR.Core.Client.Events
{
    [CreateAssetMenu(menuName = "Events/Quests/Quest Progress")]
    public class QuestProgressEvent : EventChannelSO<QuestProgressData> { }
}
