using Game.Core.Interactions;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Npc Interacted")]
    public class NpcInteractedEvent : EventChannelSO<NpcInteractedData> { }
}
