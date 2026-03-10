using UnityEngine;

namespace FTR.Core.Server
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Server/Prefab Provider")]
    public class ServerPrefabProvider : ScriptableObject
    {
        public GameObject ServerCharacterComponents;
        public GameObject ServerLootItemComponents;
    }
}
