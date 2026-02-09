using Game.Core.Client.Interactions;
using Game.Core.Common.Events;
using UnityEngine;

namespace Game.Core.Client.Events
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Npc Interacted")]
    public class NpcInteractedEvent : EventChannelSO<NpcInteractedData> { }
}
