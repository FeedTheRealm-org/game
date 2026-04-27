using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    /// <summary>
    /// Raised by InteractView when the server closes a dialog sequence (IsInteracting → false).
    /// Subscribed by CharacterInteractingState to know when to exit the state
    /// </summary>
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Npc Dialog Closed")]
    public class NpcDialogClosedEvent : EventChannelSO { }
}
