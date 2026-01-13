using Game.Core.Quests;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(menuName = "Events/Quests/Quest Progress")]
    public class QuestProgressEvent : EventChannelSO<QuestProgressData> { }
}
