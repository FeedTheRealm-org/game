using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Common.Environment.Chests
{
    public class ChestController : MonoBehaviour
    {
        public ChestData chestData;
        private GameObject openChestVisual;
        private GameObject closedChestVisual;

        public void Initialize(ChestData chestData)
        {
            this.chestData = chestData;
            transform.position = chestData.position;
            transform.rotation = Quaternion.Euler(chestData.rotation);
            transform.localScale = chestData.size;
        }

        public void ToggleChestState(bool isOpen)
        {
            openChestVisual.SetActive(isOpen);
            closedChestVisual.SetActive(!isOpen);
        }

        public void SetupMesh(GameObject openChestVisual, GameObject closedChestVisual)
        {
            this.openChestVisual = SetupChestVisuals(
                openChestVisual,
                chestData.opendedChestModelData
            );
            this.closedChestVisual = SetupChestVisuals(
                closedChestVisual,
                chestData.closedChestModelData
            );
            this.openChestVisual.SetActive(false);
            this.closedChestVisual.SetActive(true);
        }

        private GameObject SetupChestVisuals(GameObject visual, ChestModelData chestModelData)
        {
            var visualInstance = Instantiate(visual, transform);
            visualInstance.SetActive(true);
            visualInstance.transform.localPosition = chestModelData.relativePosition;
            visualInstance.transform.localRotation = Quaternion.Euler(
                chestModelData.relativeRotation
            );
            visualInstance.transform.localScale = chestModelData.relativeSize;
            return visualInstance;
        }

        private void OnDrawGizmos()
        {
            if (chestData == null)
                return;

            Gizmos.color = Color.orange;
            Gizmos.matrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                transform.localScale
            );
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
