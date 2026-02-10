using Mirror;
using Models;
using UnityEngine;

public class StructureController : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnStructureDataChanged))]
    private StructureData structureData;

    private BoxCollider boxCollider;
    private GameObject visualInstance;

    #region Server

    [Server]
    public void Initialize(StructureData data)
    {
        structureData = data;

        transform.position = data.position;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        SetupServerCollider();
    }

    [Server]
    private void SetupServerCollider()
    {
        boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.size = structureData.colliderSize;
        boxCollider.center = structureData.colliderCenter;
        boxCollider.isTrigger = false;
    }

    #endregion

    #region Client

    private void OnStructureDataChanged(StructureData oldData, StructureData newData)
    {
        structureData = newData;
        if (visualInstance != null)
        {
            visualInstance.transform.localPosition = newData.offset;
            visualInstance.transform.localRotation = Quaternion.Euler(newData.rotation);
            visualInstance.transform.localScale = newData.size;
        }
    }

    [Client]
    public void RenderVisual(GameObject modelPrefab)
    {
        if (visualInstance != null)
            Destroy(visualInstance);

        visualInstance = Instantiate(modelPrefab, transform);
        visualInstance.SetActive(true);

        visualInstance.transform.localPosition = structureData.offset;
        visualInstance.transform.localRotation = Quaternion.Euler(structureData.rotation);
        visualInstance.transform.localScale = structureData.size;

        EnsureClientCollider();
    }

    [Client]
    private void EnsureClientCollider()
    {
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider>();

        boxCollider.size = structureData.colliderSize;
        boxCollider.center = structureData.colliderCenter;
    }

    #endregion

    #region Public API
    public string ModelId => structureData.id;
    #endregion
}
