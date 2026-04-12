using System;

namespace FTR.Core.Common.Characters
{
    public interface ICharacterIdentity
    {
        string CharacterId { get; }
        event Action<string> OnCharacterIdChanged;

        string CharacterName { get; }
        event Action<string> OnCharacterNameChanged;

        bool IsLocalPlayer { get; }
    }
}
