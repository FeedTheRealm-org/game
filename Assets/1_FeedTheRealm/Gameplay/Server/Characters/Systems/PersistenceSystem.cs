using FTR.Core.Common.Utils;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class PersistenceSystem : MonoBehaviour, IGameTickable
    {
        [Inject]
        private PlayerSpawnpointManager playerSpawnpointManager;

        public void Initialize(MovementSystem movementSystem, InventorySystem inventorySystem)
        {
            LoadPlayerPosition(movementSystem);
            LoadPlayerInventory(inventorySystem);
        }

        private void LoadPlayerPosition(MovementSystem movementSystem)
        {
            Vector3 spawnPoint = playerSpawnpointManager.GetRandomSpawnpoint();
            movementSystem.LoadPosition(spawnPoint);
        }

        private void LoadPlayerInventory(InventorySystem inventorySystem)
        {
            // TODO: call the db to get the player's inventory, for now we will just initialize with an empty inventory
            // inventorySystem.LoadInventory(new string[0], new string[0]);
        }

        public void GameTick(float dt) { }
    }
}
