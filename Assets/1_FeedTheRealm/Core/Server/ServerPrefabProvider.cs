using UnityEngine;

namespace FTR.Core.Server
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Server/Prefab Provider")]
    public class ServerPrefabProvider : ScriptableObject
    {
        public GameObject ServerCharacterComponents;
        public GameObject ServerPlayerComponents;
        public GameObject ServerLootItemComponents;
        public GameObject ServerEnemyComponents;
        public GameObject ServerNpcComponents;
        public GameObject ServerShopComponent;
        public GameObject PortalComponent;
        public GameObject ChestComponent;
        public GameObject LootItemPrefab;

        [Header("Loader Components")]
        public GameObject StructureComponent;
        public GameObject AggresiveNpcSpawnerComponent;
        public GameObject FriendlyNpcSpawnerComponent;
        public GameObject ShopComponent;
        public GameObject PortalPrefab;
        public GameObject ChestPrefab;

        [Header("Utility")]
        public GameObject PlayerTriggerAreaPrefab;
    }
}
