using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Quest
{
    [CreateAssetMenu(
        fileName = "NpcQuestOfferedEvent",
        menuName = "Events/Client/Quest/Npc Quest Offered"
    )]
    public class NpcQuestOfferedEvent : EventChannelSO<string> { }
}
