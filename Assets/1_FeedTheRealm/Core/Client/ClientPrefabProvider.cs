using UnityEngine;

namespace FTR.Core.Client
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Client/Prefab Provider")]
    public class ClientPrefabProvider : ScriptableObject
    {
        public GameObject ClientCharacterComponents;
        public GameObject HudComponent;
        public GameObject LootItemVisual;
        public GameObject InventoryHudComponent;
        public GameObject DialogBox;
        public GameObject SettingMenuComponent;
        public GameObject QuestPrompt;
        public GameObject QuestCompletionPanel;

        [Header("Shop Components")]
        public GameObject ShopItemVisual;
        public GameObject ShopMenuComponent;

        [Header("Loader Components")]
        public GameObject StructurePrefab;
    }
}
