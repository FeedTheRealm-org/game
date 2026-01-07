using Game.Core.Quests;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(menuName = "Events/Quests/Quest Completed")]
    public class QuestCompletedEvent : EventChannelSO<QuestData> { }
}
