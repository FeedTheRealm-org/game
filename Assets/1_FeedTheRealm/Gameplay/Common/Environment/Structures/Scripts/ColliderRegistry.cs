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
                layer = (int)Mathf.Log(config.SlopeColliderLayerMask.value, 2);
                break;
            case ColliderType.Cube:
            default:
                if (colliderType != ColliderType.Cube)
                    Debug.LogWarning(
                        $"ColliderRegistry: No prefab found for collider type {colliderType}, defaulting to cube."
                    );
                prefab = cubeColliderPrefab;
                layer = (int)Mathf.Log(config.CubeColliderLayerMask.value, 2);
                break;
        }

        var instance = Object.Instantiate(prefab);
        instance.SetActive(true);
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            renderer.enabled = config.DEBUG_EnableColliderView;

        return (instance, layer);
    }
}
