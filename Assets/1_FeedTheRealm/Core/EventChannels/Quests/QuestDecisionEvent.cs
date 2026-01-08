using Game.Core.Quests;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(menuName = "Events/Quests/Quest Decision")]
    public class QuestDecisionEvent : EventChannelSO<QuestDecisionData> { }
}
