using Enums;
using UnityEngine;

[CreateAssetMenu(fileName = "ColliderRegistry", menuName = "Scriptable Objects/Collider Registry")]
public class ColliderRegistry : ScriptableObject
{
    [SerializeField]
    private LayerMask cubeColliderLayerMask;

    [SerializeField]
    private LayerMask slopeColliderLayerMask;

    [SerializeField]
    private GameObject cubeColliderPrefab;

    [SerializeField]
    private GameObject slopeColliderPrefab;

    public (GameObject, int) GetColliderPrefab(ColliderType colliderType)
    {
        switch (colliderType)
        {
            case ColliderType.Slope:
                return (slopeColliderPrefab, (int)Mathf.Log(slopeColliderLayerMask.value, 2));
            case ColliderType.Cube:
                return (cubeColliderPrefab, (int)Mathf.Log(cubeColliderLayerMask.value, 2));
            default:
                Debug.LogWarning(
                    $"ColliderRegistry: No prefab found for collider type {colliderType}, defaulting to cube."
                );
                return (cubeColliderPrefab, (int)Mathf.Log(cubeColliderLayerMask.value, 2));
        }
    }
}
