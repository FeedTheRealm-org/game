using Game.Core.Common.Events;
using UnityEngine;

namespace Game.Core.Client.Events
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Enemy Slayed")]
    public class EnemySlayedEvent : EventChannelSO { }
}
