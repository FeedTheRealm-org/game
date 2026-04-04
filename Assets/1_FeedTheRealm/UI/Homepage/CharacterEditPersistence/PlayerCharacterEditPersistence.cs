using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerCharacterEditPersistence",
    menuName = "Scriptable Objects/Character Edit/Player Persistence"
)]
public class PlayerCharacterEditPersistence : CharacterEditPersistence
{
    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private Session.Session session;

    public override async Task<API.CharacterInfoResponse> LoadAsync()
    {
        return await playerService.GetCharacterInfoAsync();
    }

    public override async Task<API.CharacterInfoResponse> SaveAsync(
        API.PatchCharacterInfoRequest data
    )
    {
        var payload = new API.PatchCharacterInfoRequest
        {
            character_name = data.character_name,
            character_bio = data.character_bio,
            category_sprites =
                data.category_sprites != null
                    ? new Dictionary<string, string>(data.category_sprites)
                    : new Dictionary<string, string>(),
        };

        var characterInfo = await playerService.PatchCharacterInfoAsync(payload);
        if (characterInfo != null && session != null)
        {
            session.IsFirstLogin = false;
            session.CharacterName = characterInfo.character_name;
        }

        return characterInfo;
    }
}
