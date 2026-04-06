using System;

namespace FTR.Core.Common.Characters
{
    /// <summary>
    /// Abstraction over the network-aware health state of a character.
    /// Consumers can subscribe to health changes.
    /// </summary>
    public interface ICharacterHealthSource
    {
        float Health { get; }
        bool IsLocalPlayer { get; }
        event Action<float> OnHealthChanged;
    }
}
