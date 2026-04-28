using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements
{
    public abstract class EquippedItem { }

    public sealed class WeaponEquipped : EquippedItem
    {
        public readonly WeaponItemData Data;

        public WeaponEquipped(WeaponItemData data) => Data = data;
    }

    public sealed class ConsumableEquipped : EquippedItem
    {
        public readonly ConsumableItemData Data;

        public ConsumableEquipped(ConsumableItemData data) => Data = data;
    }
}
