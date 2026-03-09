using UnityEngine;

namespace FTR.Core.Server
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Server/Prefab Provider")]
    public class ServerPrefabProvider : ScriptableObject
    {
        public GameObject ServerPlayerComponents;
        public GameObject ServerLootItemPrefab;
    }
}
