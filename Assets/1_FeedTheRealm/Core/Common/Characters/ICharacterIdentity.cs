using System;

namespace FTR.Core.Common.Characters
{
    public interface ICharacterIdentity
    {
        string CharacterId { get; }
        event Action<string> OnCharacterIdChanged;
    }
}
