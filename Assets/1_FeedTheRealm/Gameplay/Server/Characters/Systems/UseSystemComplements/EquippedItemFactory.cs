using FTR.Core.Server.Config;
using FTR.Core.Server.Enums;
using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using FTR.Gameplay.Server.Registry;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements
{
    /// <summary>
    /// Centralized construction point for equipped items and their strategies.
    ///
    /// Adding a new item type:
    ///   1. Create a new IUseStrategy implementation.
    ///   2. Add a case here that produces (EquippedItem, yourStrategy).
    ///   3. Done — UseSystem never needs to change.
    /// </summary>
    public static class EquippedItemFactory
    {
        public readonly struct Result
        {
            public readonly EquippedItem Item;
            public readonly IUseStrategy Strategy;

            public Result(EquippedItem item, IUseStrategy strategy)
            {
                Item = item;
                Strategy = strategy;
            }
        }

        public static Result Build(
            string itemId,
            ServerConfig config,
            Logging.Logger logger,
            object logSource
        )
        {
            if (string.IsNullOrEmpty(itemId))
            {
                logger?.Log(
                    $"[EquippedItemFactory] No item — default: dmg={config.UnequippedDamage} range={config.UnequippedRange} cd={config.AttackCooldown}.",
                    logSource
                );
                return new Result(null, new BareHandsStrategy());
            }

            switch (ServerItemsRegistry.GetItemTypeById(itemId))
            {
                case EquipmentType.Weapon:
                    var weaponData = ServerItemsRegistry.GetWeaponById(itemId);
                    if (weaponData == null)
                    {
                        logger?.Log(
                            $"[EquippedItemFactory] No weapon data for '{itemId}'. Falling back to bare hands.",
                            logSource,
                            Logging.LogType.Warning
                        );
                        return new Result(null, new BareHandsStrategy());
                    }
                    logger?.Log(
                        $"[EquippedItemFactory] Weapon '{itemId}': dmg={weaponData.damage} range={weaponData.range} speed={weaponData.attackSpeed}.",
                        logSource
                    );
                    return new Result(
                        new WeaponEquipped(weaponData),
                        new MeleeWeaponStrategy(weaponData)
                    );

                case EquipmentType.Consumable:
                    var consumableData = ServerItemsRegistry.GetConsumableById(itemId);
                    if (consumableData == null)
                    {
                        logger?.Log(
                            $"[EquippedItemFactory] No consumable data for '{itemId}'. Falling back to bare hands.",
                            logSource,
                            Logging.LogType.Warning
                        );
                        return new Result(null, new BareHandsStrategy());
                    }
                    logger?.Log(
                        $"[EquippedItemFactory] Consumable '{itemId}': effect={consumableData.effectType} value={consumableData.value} cd={consumableData.cooldown}.",
                        logSource
                    );
                    return new Result(
                        new ConsumableEquipped(consumableData),
                        new ConsumableStrategy(consumableData)
                    );

                default:
                    logger?.Log(
                        $"[EquippedItemFactory] Unknown item type for '{itemId}'. Falling back to bare hands.",
                        logSource,
                        Logging.LogType.Warning
                    );
                    return new Result(null, new BareHandsStrategy());
            }
        }
    }
}
