using Game.Core.Client.Quests;
using Game.Core.Common.Events;
using UnityEngine;

namespace Game.Core.Client.Events
{
    [CreateAssetMenu(menuName = "Events/Quests/Quest Decision")]
    public class QuestDecisionEvent : EventChannelSO<QuestDecisionData> { }
}
