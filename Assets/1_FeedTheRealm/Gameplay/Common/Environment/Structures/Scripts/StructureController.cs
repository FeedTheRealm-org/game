using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Common.Environment.Structures
{
    public class StructureController : MonoBehaviour
    {
        private StructureData structureData;
        public StructureData Data => structureData;

        private GameObject visualInstance;
        private MeshCollider meshCollider;

        public void Initialize(StructureData structureData, GameObject visualPrefab)
        {
            this.structureData = structureData;

            transform.position = structureData.position;
            transform.rotation = Quaternion.Euler(structureData.rotation);
            transform.localScale = Vector3.one;

            SetupMesh(visualPrefab);
            SetupCollider();
        }

        private void SetupMesh(GameObject visualPrefab)
        {
            visualInstance = Instantiate(visualPrefab, transform);
            visualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visualInstance.transform.localScale = structureData.size;
        }

        private void SetupCollider()
        {
            MeshFilter meshFilter = visualInstance.GetComponentInChildren<MeshFilter>();

            if (meshFilter == null)
            {
                Debug.LogWarning("No MeshFilter found for collider.");
                return;
            }

            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
        }

        public void RemoveVisual()
        {
            if (visualInstance != null)
                Destroy(visualInstance);
        }

        private void OnDrawGizmos()
        {
            if (meshCollider == null || meshCollider.sharedMesh == null)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireMesh(
                meshCollider.sharedMesh,
                transform.position,
                transform.rotation,
                transform.lossyScale
            );
        }
    }
}
