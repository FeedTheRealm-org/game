using System;

public class StaticCharacterIdSource : ICharacterIdSource
{
    public event Action<string> OnCharacterIdChanged;

    private string characterId;
    public string CharacterId => characterId;

    public StaticCharacterIdSource(string characterId)
    {
        this.characterId = characterId;
    }
}
