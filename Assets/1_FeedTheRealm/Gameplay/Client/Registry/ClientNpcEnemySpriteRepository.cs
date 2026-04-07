using System.Collections.Generic;
using System.Threading.Tasks;

namespace FTR.Gameplay.Client.Registry
{
    public class ClientNpcEnemySpriteRepository : CharacterSpriteRepository
    {
        private const int RegistryWaitTimeoutMs = 8000;
        private const int RegistryRetryDelayMs = 100;

        public async Task<API.CharacterInfoResponse> LoadAsync(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return null;

            var characterInfo = TryBuildCharacterInfo(characterId);
            if (characterInfo != null)
                return characterInfo;

            // NPCs can be spawned on server before the client world loaders populate registries.
            // Wait briefly for world data so initial CharacterId sync can still resolve sprites.
            if (ClientItemsRegistry.CurrentWorldData == null)
            {
                var waitedMs = 0;
                while (
                    ClientItemsRegistry.CurrentWorldData == null && waitedMs < RegistryWaitTimeoutMs
                )
                {
                    await Task.Delay(RegistryRetryDelayMs);
                    waitedMs += RegistryRetryDelayMs;

                    characterInfo = TryBuildCharacterInfo(characterId);
                    if (characterInfo != null)
                        return characterInfo;
                }
            }

            return TryBuildCharacterInfo(characterId);
        }

        private static API.CharacterInfoResponse TryBuildCharacterInfo(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return null;

            var npcData = ClientItemsRegistry.GetNpcById(characterId);
            if (npcData != null)
            {
                return BuildCharacterInfo(
                    npcData.name,
                    npcData.description,
                    npcData.category_sprites
                );
            }

            var enemyData = ClientItemsRegistry.GetEnemyById(characterId);
            if (enemyData != null)
            {
                return BuildCharacterInfo(
                    enemyData.name,
                    enemyData.description,
                    enemyData.category_sprites
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
            Dictionary<string, string> categorySprites
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
            };
        }
    }
}
