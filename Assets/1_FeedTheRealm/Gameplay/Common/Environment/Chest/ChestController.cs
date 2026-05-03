using FTR.Gameplay.Common.NetworkEntities.Chest;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Common.Environment.Chests
{
    public class ChestController : MonoBehaviour
    {
        public ChestData chestData;
        private ChestStateStorage chestStateStorage;

        public void Initialize(ChestStateStorage chestStateStorage)
        {
            chestData = chestStateStorage.ChestData;
            gameObject.name = $"Chest-{chestData.name}-{chestData.id}";
            transform.position = chestData.position;
            transform.rotation = Quaternion.Euler(chestData.rotation);
            transform.localScale = chestData.size;
            this.chestStateStorage = chestStateStorage;
        }

        private void OnDrawGizmos()
        {
            if (chestData == null)
                return;

            Gizmos.color = chestStateStorage.IsOpen ? Color.green : Color.orange;
            Gizmos.matrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                transform.localScale
            );
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
