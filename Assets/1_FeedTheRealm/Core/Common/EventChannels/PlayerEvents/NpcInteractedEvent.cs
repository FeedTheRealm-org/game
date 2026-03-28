using FTR.Core.Common.Interactions;
using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Npc Interacted")]
    public class NpcInteractedEvent : EventChannelSO<NpcInteractedData> { }
}
