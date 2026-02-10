using FTR.Core.Client.Interactions;
using FTR.Core.Common.Events;
using UnityEngine;

namespace FTR.Core.Client.Events
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Npc Interacted")]
    public class NpcInteractedEvent : EventChannelSO<NpcInteractedData> { }
}
