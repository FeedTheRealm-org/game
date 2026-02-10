using System;
using UnityEngine;

namespace FTR.Core.Common.Events
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

    /// <summary>
    /// A ScriptableObject that acts as an event channel for broadcasting events with no payload.
    /// </summary>
    public abstract class EventChannelSO : ScriptableObject
    {
        public event Action OnRaised;

        public void Raise()
        {
            OnRaised?.Invoke();
        }
    }
}
