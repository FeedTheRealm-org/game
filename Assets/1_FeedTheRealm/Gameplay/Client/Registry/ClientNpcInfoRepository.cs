using System.Collections.Generic;
using System.Threading.Tasks;
using API;

namespace FTR.Gameplay.Client.Registry
{
    public class ClientNpcInfoRepository : CharacterInfoRepository
    {
        private const int RegistryWaitTimeoutMs = 8000;
        private const int RegistryRetryDelayMs = 100;

        public async Task<API.CharacterInfoResponse> LoadAsync(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return null;

            var npcData = ClientItemsRegistry.GetNpcById(characterId);
            if (npcData != null)
            {
                return BuildCharacterInfo(
                    npcData.name,
                    npcData.description,
                    npcData.category_sprites,
                    npcData.skin_color,
                    npcData.hair_color,
                    npcData.eye_color
                );
            }

            var enemyData = ClientItemsRegistry.GetEnemyById(characterId);
            if (enemyData != null)
            {
                return BuildCharacterInfo(
                    enemyData.name,
                    enemyData.description,
                    enemyData.category_sprites,
                    enemyData.skin_color,
                    enemyData.hair_color,
                    enemyData.eye_color
                );
            }

            return null;
        }

        public Task<API.CharacterInfoResponse> SaveAsync(
            string characterId,
            API.PatchCharacterInfoRequest data
        )
        {
            return LoadAsync(characterId);
        }

        private static API.CharacterInfoResponse BuildCharacterInfo(
            string characterName,
            string characterBio,
            Dictionary<string, string> categorySprites,
            CharacterColorHsv skin_color,
            CharacterColorHsv hair_color,
            CharacterColorHsv eye_color
        )
        {
            return new API.CharacterInfoResponse
            {
                character_name = characterName,
                character_bio = characterBio,
                category_sprites =
                    categorySprites != null
                        ? new Dictionary<string, string>(categorySprites)
                        : new Dictionary<string, string>(),
                skin_color = skin_color,
                hair_color = hair_color,
                eye_color = eye_color,
            };
        }
    }
}
