using System;
using UnityEngine;

namespace Game.Core.Events
{
    /// <summary>
    /// A ScriptableObject that acts as an event channel for broadcasting events with a payload of type T.
    /// </summary>
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        public event Action<T> OnRaised;

        public void Raise(T value)
        {
            OnRaised?.Invoke(value);
        }
    }
}
