using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Npc Dialog Toggled")]
    public class NpcDialogToggledEvent : EventChannelSO<(bool isOpen, string npcId)> { }
}
