using System;
using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    /// <summary>
    /// Event channel for toggling the Quest Journal menu.
    /// Subscribe to OnRaised to show/hide the full quest list menu.
    /// </summary>
    [CreateAssetMenu(
        fileName = "QuestMenuToggleEvent",
        menuName = "Events/Client/UI/Quest Toggle Event"
    )]
    public class QuestMenuToggleEvent : ScriptableObject
    {
        public event Action OnRaised;

        public void Raise()
        {
            OnRaised?.Invoke();
        }
    }
}
