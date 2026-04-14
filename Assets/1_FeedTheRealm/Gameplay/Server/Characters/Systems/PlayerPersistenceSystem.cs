using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class PlayerPersistenceSystem : MonoBehaviour
    {
        private InventorySystem inventorySystem;
        private GoldSystem goldSystem;
        private MovementSystem movementSystem;
        private QuestSystem questSystem;

        public void Initialize(
            InventorySystem inventorySystem,
            GoldSystem goldSystem,
            MovementSystem movementSystem,
            QuestSystem questSystem
        )
        {
            this.inventorySystem = inventorySystem;
            this.goldSystem = goldSystem;
            this.movementSystem = movementSystem;
            this.questSystem = questSystem;
        }

        // public void SaveQuestProgress(QuestProgressState progressState)
        // {
        // }

        // public QuestProgressState LoadQuestProgress()
        // {
        // }

        // public void SaveInventory(InventoryStateStorage inventoryState)
        // {
        // }

        // public InventoryStateStorage LoadInventory()
        // {
        // }

        // public void SaveGold(int goldAmount)
        // {
        // }

        // public int LoadGold()
        // {
        // }

        // public void SavePosition(Vector3 position)
        // {
        // }

        // public Vector3 LoadPosition()
        // {
        // }
    }
}
