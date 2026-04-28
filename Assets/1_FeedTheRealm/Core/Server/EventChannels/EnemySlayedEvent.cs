using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(
        fileName = "EnemySlayedEvent",
        menuName = "Events/Server/Quests/EnemySlayedEvent"
    )]
    public class EnemySlayedEvent : EventChannelSO<(uint killerNetId, string enemyTypeId)> { }
}
