using Enums;
using FTR.Core.Common.Config;
using UnityEngine;

[CreateAssetMenu(fileName = "ColliderRegistry", menuName = "Scriptable Objects/Collider Registry")]
public class ColliderRegistry : ScriptableObject
{
    [SerializeField]
    private Config config;

    [SerializeField]
    private GameObject cubeColliderPrefab;

    [SerializeField]
    private GameObject slopeColliderPrefab;

    public (GameObject, int) GetColliderPrefab(ColliderType colliderType)
    {
        GameObject prefab;
        int layer;

        switch (colliderType)
        {
            case ColliderType.Slope:
                prefab = slopeColliderPrefab;
                // TODO(refactor): consider changind this by using something like "LayerMask.NameToLayer(GroundLayer)"
                layer = (int)Mathf.Log(config.slopeColliderLayerMask.value, 2);
                break;
            case ColliderType.Cube:
            default:
                if (colliderType != ColliderType.Cube)
                    Debug.LogWarning(
                        $"ColliderRegistry: No prefab found for collider type {colliderType}, defaulting to cube."
                    );
                prefab = cubeColliderPrefab;
                layer = (int)Mathf.Log(config.cubeColliderLayerMask.value, 2);
                break;
        }
        prefab.SetActive(true);
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>())
            renderer.enabled = config.enableColliderView;

        return (prefab, layer);
    }
}
