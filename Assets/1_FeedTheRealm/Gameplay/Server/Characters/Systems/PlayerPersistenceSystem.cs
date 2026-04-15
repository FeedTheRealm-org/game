using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FTR.Core.Server.Persistence;
using FTR.Core.Server.Persistence.Schemas;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class PlayerPersistenceSystem : MonoBehaviour
    {
        [Inject]
        private Logging.Logger logger;

        [Inject]
        private PlayersRepository playersRepository;

        private CharacterStateStorage characterStateStorage;
        private InventorySystem inventorySystem;
        private GoldSystem goldSystem;
        private MovementSystem movementSystem;
        private QuestSystem questSystem;

        public void Initialize(
            CharacterStateStorage characterStateStorage,
            InventorySystem inventorySystem,
            GoldSystem goldSystem,
            MovementSystem movementSystem,
            QuestSystem questSystem
        )
        {
            this.characterStateStorage = characterStateStorage;
            this.inventorySystem = inventorySystem;
            this.goldSystem = goldSystem;
            this.movementSystem = movementSystem;
            this.questSystem = questSystem;

            // this.inventorySystem.OnSaveInventory += SaveInventory;
            // this.goldSystem.OnSaveGold += SaveGold;
            // this.questSystem += SaveQuestProgress;

            if (!string.IsNullOrEmpty(characterStateStorage.CharacterId))
                LoadPlayer(characterStateStorage.CharacterId).Forget();
            else
                characterStateStorage.OnCharacterIdChanged += OnCharacterIdChanged;
        }

        private void OnDestroy()
        {
            // this.inventorySystem.OnSaveInventory -= SaveInventory;
            // this.goldSystem.OnSaveGold -= SaveGold;
            // this.questSystem -= SaveQuestProgress;
            // characterStateStorage.OnPositionCorrected -= SavePosition;
            characterStateStorage.OnCharacterIdChanged -= OnCharacterIdChanged;
        }

        /// <summary>
        /// Handles the player's quest progress Persistence to the repository.
        /// (effectiveQuestId = $"{QuestId}_{NpcId}")
        /// </summary>
        public void SaveQuestProgress(string effectiveQuestId, int progress, bool completed)
        {
            if (string.IsNullOrEmpty(characterStateStorage.CharacterId))
                return;
            playersRepository
                .SaveQuestAsync(
                    characterStateStorage.CharacterId,
                    effectiveQuestId,
                    progress,
                    completed
                )
                .AsUniTask()
                .Forget(ex => logger.Log($"SaveQuestProgress failed: {ex}", Logging.LogType.Error));
        }

        /// <summary>
        /// Handles the player's inventory and fastAccess Persistence to the repository.
        /// </summary>
        public void SaveInventory(InventoryItemModel[] inventory, InventoryItemModel[] fastAccess)
        {
            if (string.IsNullOrEmpty(characterStateStorage.CharacterId))
                return;
            var inventoryList = new List<InventoryItemModel>(inventory);
            var fastAccessList = new List<InventoryItemModel>(fastAccess);
            UniTask
                .WhenAll(
                    playersRepository
                        .SaveInventoryAsync(characterStateStorage.CharacterId, inventoryList)
                        .AsUniTask(),
                    playersRepository
                        .SaveFastAccessAsync(characterStateStorage.CharacterId, fastAccessList)
                        .AsUniTask()
                )
                .Forget(ex => logger.Log($"SaveInventory failed: {ex}", Logging.LogType.Error));
        }

        /// <summary>
        /// Handles the player's gold Persistence to the repository.
        /// </summary>
        public void SaveGold(int goldAmount)
        {
            if (string.IsNullOrEmpty(characterStateStorage.CharacterId))
                return;
            playersRepository
                .SaveGoldAsync(characterStateStorage.CharacterId, goldAmount)
                .AsUniTask()
                .Forget(ex => logger.Log($"SaveGold failed: {ex}", Logging.LogType.Error));
        }

        /// <summary>
        /// Handles the player's position Persistence to the repository.
        /// </summary>
        public void SavePosition(Vector3 position)
        {
            if (string.IsNullOrEmpty(characterStateStorage.CharacterId))
                return;
            playersRepository
                .SavePositionAsync(
                    characterStateStorage.CharacterId,
                    new PositionModel
                    {
                        X = position.x,
                        Y = position.y,
                        Z = position.z,
                    }
                )
                .AsUniTask()
                .Forget(ex => logger.Log($"SavePosition failed: {ex}", Logging.LogType.Error));
        }

        private void OnCharacterIdChanged(string characterId) => LoadPlayer(characterId).Forget();

        private async UniTask LoadPlayer(string playerId)
        {
            var player = await playersRepository.GetPlayerAsync(playerId).AsUniTask();
            if (player == null)
            {
                logger.Log($"No saved data found for player {playerId}");
                return;
            }
            // inventorySystem.LoadInventory();
            // goldSystem.LoadGold();
            // questSystem.LoadQuestProgress();
            // movementSystem.LoadPosition();
        }
    }
}
