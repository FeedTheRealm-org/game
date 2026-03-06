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

        public void RenderVisual(GameObject referenceModel)
        {
            GameObject structureModel = Instantiate(referenceModel);
            structureModel.SetActive(true);

            structureModel.transform.parent = gameObject.transform;
            structureModel.transform.localPosition = Vector3.zero;
            structureModel.transform.localRotation = Quaternion.identity;
            structureModel.transform.localScale = structureData.size;
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
