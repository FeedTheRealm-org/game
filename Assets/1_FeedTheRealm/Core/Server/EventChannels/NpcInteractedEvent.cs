using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Server.EventChannels
{
    [CreateAssetMenu(
        fileName = "NpcInteractedEvent",
        menuName = "Events/Server/Quests/NpcInteractedEvent"
    )]
    public class NpcInteractedEvent : EventChannelSO<(uint playerNetId, string npcId)> { }
}
