using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Common.Environment.Structures
{
    public class StructureController : MonoBehaviour
    {
        private StructureData structureData;
        public StructureData Data => structureData;

        public void Initialize(StructureData structureData)
        {
            this.structureData = structureData;

            transform.position = structureData.position;
            transform.rotation = Quaternion.Euler(structureData.rotation);
            transform.localScale = structureData.size;
            SetupCollider();
        }

        private void SetupCollider()
        {
            var boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = structureData.colliderSize;
            boxCollider.center = structureData.colliderCenter;
            boxCollider.includeLayers = LayerMask.GetMask("Player");
        }

        public void SetupMesh(GameObject visualPrefab)
        {
            var visualInstance = Instantiate(visualPrefab, transform);
            visualInstance.SetActive(true);
            visualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visualInstance.transform.localScale = Vector3.one;
        }

        private void OnDrawGizmos()
        {
            if (structureData == null)
                return;

            Gizmos.color = Color.silver;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                transform.localScale
            );
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(structureData.colliderCenter, structureData.colliderSize);
        }
    }
}
