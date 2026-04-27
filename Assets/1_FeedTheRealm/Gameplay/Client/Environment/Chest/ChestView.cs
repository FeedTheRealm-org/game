using FTR.Gameplay.Common.NetworkEntities.Chest;
using UnityEngine;

namespace FTR.Gameplay.Environment.Chest
{
    /// <summary>
    /// View component for the Chest. Depending on the value in the ChestStateStorage,
    /// it will display the chest as open or closed. Lives on the Chest prefab as a child of the main Chest GameObject.
    /// </summary>
    public class ChestView : MonoBehaviour
    {
        private GameObject openChestVisual;
        private GameObject closedChestVisual;

        private ChestStateStorage chestStateStorage;

        public void SetupModel(GameObject openChestVisual, GameObject closedChestVisual)
        {
            this.openChestVisual = openChestVisual;
            this.closedChestVisual = closedChestVisual;
        }

        public void Initialize(ChestStateStorage chestStateStorage)
        {
            this.chestStateStorage = chestStateStorage;
            chestStateStorage.OnChestStateChanged += HandleChestOpenStateChanged;
            UpdateChestVisual(chestStateStorage.IsOpen);
        }

        private void HandleChestOpenStateChanged(bool isOpen)
        {
            UpdateChestVisual(isOpen);
        }

        private void UpdateChestVisual(bool isOpen)
        {
            openChestVisual.SetActive(isOpen);
            closedChestVisual.SetActive(!isOpen);
        }

        void OnDestroy()
        {
            chestStateStorage.OnChestStateChanged -= HandleChestOpenStateChanged;
        }
    }
}
