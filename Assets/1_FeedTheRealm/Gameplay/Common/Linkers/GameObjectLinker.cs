using FTR.Core.Common.Enums;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Common.Linkers
{
    public class GameObjectLinker : MonoBehaviour
    {
        [Inject]
        private PlayerLinker playerLinker;

        [Inject]
        private AggresiveNpcLinker aggresiveNpcLinker;

        [Inject]
        private PassiveNpcLinker passiveNpcLinker;

        [Inject]
        private LootItemLinker lootItemLinker;

        [Inject]
        private ShopLinker shopLinker;

        [SerializeField]
        private LinkerType linkerType;

        public void Initialize()
        {
            switch (linkerType)
            {
                case LinkerType.Player:
                    playerLinker.Link(gameObject);
                    break;
                case LinkerType.AggresiveNPC:
                    aggresiveNpcLinker.Link(gameObject);
                    break;
                case LinkerType.PassiveNPC:
                    passiveNpcLinker.Link(gameObject);
                    break;
                case LinkerType.LootItem:
                    lootItemLinker.Link(gameObject);
                    break;
                case LinkerType.Shop:
                    shopLinker.Link(gameObject);
                    break;
                default:
                    throw new System.ArgumentException($"Unsupported LinkerType: {linkerType}");
            }
        }
    }
}
