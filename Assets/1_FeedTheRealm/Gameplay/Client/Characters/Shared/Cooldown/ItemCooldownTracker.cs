using System.Collections.Generic;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Characters.Shared.Cooldown
{
    public class ItemCooldownTracker : MonoBehaviour
    {
        private NetworkEventRouter eventRouter;
        private CharacterStateStorage stateStorage;
        private CooldownStartedEvent cooldownStartedEvent;

        private WeaponItemData weaponData;
        private ConsumableItemData consumableData;

        private readonly Dictionary<string, int> shotCounts = new();

        public void Initialize(
            NetworkEventRouter eventRouter,
            CharacterStateStorage stateStorage,
            CooldownStartedEvent cooldownStartedEvent
        )
        {
            this.eventRouter = eventRouter;
            this.stateStorage = stateStorage;
            this.cooldownStartedEvent = cooldownStartedEvent;

            eventRouter.OnAttackEvent += OnAttackEvent;
            stateStorage.OnEquippedItemChanged += OnEquippedItemChanged;
        }

        private void OnDestroy()
        {
            if (eventRouter != null)
                eventRouter.OnAttackEvent -= OnAttackEvent;
            if (stateStorage != null)
                stateStorage.OnEquippedItemChanged -= OnEquippedItemChanged;
        }

        private void OnEquippedItemChanged(string itemId)
        {
            weaponData = null;
            consumableData = null;

            if (string.IsNullOrEmpty(itemId))
                return;

            weaponData = ClientItemsRegistry.GetWeaponById(itemId);
            if (weaponData == null)
                consumableData = ClientItemsRegistry.GetConsumableById(itemId);
        }

        private void OnAttackEvent(AttackEventContent _)
        {
            if (weaponData != null && weaponData.attackSpeed > 0f)
            {
                cooldownStartedEvent?.Raise((weaponData.id, GetWeaponCooldownDuration()));
            }
            else if (consumableData != null && consumableData.cooldown > 0f)
            {
                cooldownStartedEvent?.Raise((consumableData.id, consumableData.cooldown));
            }
        }

        private float GetWeaponCooldownDuration()
        {
            if (weaponData.weaponType == WeaponType.Ranged && weaponData.ammo > 0)
            {
                shotCounts.TryGetValue(weaponData.id, out int shotCount);
                shotCount++;
                if (shotCount >= weaponData.ammo)
                {
                    shotCounts[weaponData.id] = 0;
                    return weaponData.reloadSpeed > 0f
                        ? weaponData.reloadSpeed
                        : weaponData.attackSpeed;
                }
                shotCounts[weaponData.id] = shotCount;
            }
            return weaponData.attackSpeed;
        }
    }
}
