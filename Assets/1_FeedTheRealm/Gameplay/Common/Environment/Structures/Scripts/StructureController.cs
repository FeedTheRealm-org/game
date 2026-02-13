using Models;
using UnityEngine;

namespace FTR.Gameplay.Common.Environment.Structures;

public class StructureController : MonoBehaviour
{
    private StructureData data;

    public void Initialize(StructureData data)
    {
        this.data = data;

        transform.position = data.position;
        transform.rotation = Quaternion.Euler(data.rotation);
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
        structureModel.transform.localScale = data.size;
        // TODO: check if scale is applied correctly, should it be applied to the parent object instead?
    }

    private void SetupCollider()
    {
        BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
        boxCollider.size = data.colliderSize;
        boxCollider.center = data.colliderCenter;
        boxCollider.isTrigger = false;
    }
}
