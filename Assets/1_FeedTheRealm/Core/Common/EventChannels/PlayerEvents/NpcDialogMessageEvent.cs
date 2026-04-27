using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Npc Dialog Message")]
    public class NpcDialogMessageEvent : EventChannelSO<(string npcId, MessageData message)> { }
}
