using FTR.Core.Client.Interactions;
using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Npc Interacted")]
    public class NpcInteractedEvent : EventChannelSO<NpcInteractedData> { }
}
