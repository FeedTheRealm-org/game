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
            transform.position = this.structureData.position;
            transform.rotation = Quaternion.Euler(this.structureData.rotation);
            transform.localScale = Vector3.one;

            SetupCollider();
        }

        public void SetVisualModel(GameObject visualModel)
        {
            visualModel.transform.SetParent(gameObject.transform);
            visualModel.SetActive(true);
            visualModel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visualModel.transform.localScale = structureData.size;
        }

        private void SetupCollider()
        {
            BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
            boxCollider.size = structureData.colliderSize;
            boxCollider.center = structureData.colliderCenter;
            boxCollider.isTrigger = false;
        }
    }
}
