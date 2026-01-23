using Game.Core.Quests;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(menuName = "Events/Quests/Quest Offered")]
    public class QuestOfferedEvent : EventChannelSO<QuestData> { }
}
