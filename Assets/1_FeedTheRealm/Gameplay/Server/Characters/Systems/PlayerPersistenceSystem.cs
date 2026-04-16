using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Interfaces;
using FTR.Core.Server.Config;
using FTR.Core.Server.Persistence;
using FTR.Core.Server.Persistence.Schemas;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class PlayerPersistenceSystem : MonoBehaviour, IPlayerSaveAllHandler
    {
        [Inject]
        private Logging.Logger logger;

        [Inject]
        private PlayersRepository playersRepository;

        [Inject]
        PlayerSpawnpointManager playerSpawnpointManager;

        [Inject]
        private ServerConfig serverConfig;

        private CharacterStateStorage characterStateStorage;
        private InventorySystem inventorySystem;
        private GoldSystem goldSystem;
        private MovementSystem movementSystem;
        private QuestSystem questSystem;
        private bool hasSavedOnDisconnect;

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

            this.inventorySystem.OnSaveInventory += SaveInventory;
            this.goldSystem.OnSaveGold += SaveGold;
            this.questSystem.OnSaveQuestProgress += SaveQuestProgress;

            if (!string.IsNullOrEmpty(characterStateStorage.CharacterId))
                LoadPlayer(characterStateStorage.CharacterId).Forget();
            else
                characterStateStorage.OnCharacterIdChanged += OnCharacterIdChanged;
        }

        private void OnDestroy()
        {
            this.inventorySystem.OnSaveInventory -= SaveInventory;
            this.goldSystem.OnSaveGold -= SaveGold;
            this.questSystem.OnSaveQuestProgress -= SaveQuestProgress;
            this.characterStateStorage.OnCharacterIdChanged -= OnCharacterIdChanged;
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

        public void SaveAll()
        {
            SaveInventory(
                inventorySystem.GetCurrentInventory(),
                inventorySystem.GetCurrentFastAccess()
            );
            SaveGold(goldSystem.GetCurrentGold());
            // SaveQuestProgress() is called on each quest progress change, so no need to call it here.
            SavePosition(movementSystem.GetCurrentPosition());
        }

        private void OnCharacterIdChanged(string characterId) => LoadPlayer(characterId).Forget();

        private async UniTask LoadPlayer(string playerId)
        {
            var player = await playersRepository.GetPlayerAsync(playerId).AsUniTask();
            if (player == null)
            {
                LoadDefaultStates();
                logger.Log($"No saved data found for player {playerId} - default states");
                return;
            }

            inventorySystem.LoadInventory(
                ToSizedArray(player.Inventory, serverConfig.InventorySize),
                ToSizedArray(player.FastAccessInventory, serverConfig.FastSlotSize)
            );
            goldSystem.LoadGold(player.Gold);
            // questSystem.LoadQuestProgress();
            movementSystem.LoadPosition(
                player.LastPosition != null
                    ? new Vector3(
                        player.LastPosition.X,
                        player.LastPosition.Y,
                        player.LastPosition.Z
                    )
                    : playerSpawnpointManager.GetRandomSpawnpoint()
            );
        }

        private void LoadDefaultStates()
        {
            inventorySystem.LoadInventory(
                ToSizedArray(null, serverConfig.InventorySize),
                ToSizedArray(null, serverConfig.FastSlotSize)
            );
            goldSystem.LoadGold(0);
            // questSystem.LoadQuestProgress(new Dictionary<string, (int progress, bool completed)>());
            movementSystem.LoadPosition(playerSpawnpointManager.GetRandomSpawnpoint());
        }

        private InventoryItemModel[] ToSizedArray(List<InventoryItemModel> source, int size)
        {
            var result = new InventoryItemModel[size];
            for (int i = 0; i < size; i++)
                result[i] =
                    (source != null && i < source.Count)
                        ? source[i]
                        : new InventoryItemModel { ItemId = string.Empty, Quantity = 0 };
            return result;
        }
    }
}
