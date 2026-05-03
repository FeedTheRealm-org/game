using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Common.Environment.Structures
{
    public class StructureController : MonoBehaviour
    {
        private StructureData structureData;
        public StructureData Data => structureData;
        private GameObject colliderInstance;

        public void Initialize(
            StructureData structureData,
            GameObject colliderPrefab,
            int colliderLayer
        )
        {
            this.structureData = structureData;

            transform.position = structureData.position;
            transform.rotation = Quaternion.Euler(structureData.rotation);
            transform.localScale = structureData.size;
            SetupCollider(colliderPrefab, colliderLayer);
        }

        public void Initialize(StructureData structureData)
        {
            transform.position = structureData.position;
            transform.rotation = Quaternion.Euler(structureData.rotation);
            transform.localScale = structureData.size;

            var boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = structureData.colliderSize;
            boxCollider.center = structureData.colliderCenter;
        }

        private void SetupCollider(GameObject colliderPrefab, int layer)
        {
            if (!structureData.hasColliders)
                return;

            colliderInstance = Instantiate(colliderPrefab, transform);

            transform.gameObject.layer = layer;
            foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;

            colliderInstance.transform.localPosition = structureData.colliderCenter;
            colliderInstance.transform.localRotation = Quaternion.Euler(
                structureData.colliderRotation
            );
            colliderInstance.transform.localScale = structureData.colliderSize;
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
            if (structureData == null || colliderInstance == null)
                return;

            Gizmos.color = Color.green;
            var meshFilter = colliderInstance.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
            {
                Gizmos.matrix = meshFilter.transform.localToWorldMatrix;
                Gizmos.DrawWireMesh(meshFilter.sharedMesh);
            }
        }
    }
}
