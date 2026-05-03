using System;
using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Scopes;
using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Chest;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using FTR.Gameplay.Server.Registry;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Environment.Chest
{
    /// <summary>
    /// Server-side chest system. Handles teleportation logic and validation.
    /// </summary>
    public class ChestInteractSystem : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private ServerConfig serverConfig;

        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ObjectResolverContainer resolverContainer;

        [SerializeField]
        private float chestTopOffset = 0.5f;

        private WorldMonitor worldMonitor;

        private ChestStateStorage chestStateStorage;
        private ServerPrefabProvider prefabProvider;
        private Vector3 chestTopPosition;
        private string chestId => chestStateStorage.ChestData.id;
        private string lootTableId => chestStateStorage.ChestData.lootTableId;
        private float chestResetTimeSeconds =>
            chestStateStorage.ChestData.chestCooldownMinutes * 60f;

        public bool CanInteract(IInteractor interactor)
        {
            return true;
        }

        public void StopInteraction(IInteractor interactor)
        {
            return;
        }

        public void Initialize(ChestStateStorage chestStateStorage, WorldMonitor worldMonitor)
        {
            this.chestStateStorage = chestStateStorage;
            this.worldMonitor = worldMonitor;
            prefabProvider = resolverContainer.Resolver.Resolve<ServerPrefabProvider>();
            GetChestTopPosition();
        }

        public string Interact(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;
            var connId = GetPlayerConnectionId(playerNetId);
            OpenChest(playerNetId);
            worldMonitor.Events.Enqueue(new InteractCompletedEvent(playerNetId, connId.Value));
            return chestId;
        }

        private void OpenChest(uint playerNetId)
        {
            if (chestStateStorage.IsOpen)
            {
                logger.Log($"Chest {chestId} is already open. Ignoring interaction.");
                return;
            }
            logger.Log($"Player {playerNetId} opened chest {chestId}.");
            chestStateStorage.SetChestState(true);
            OnOpenChest();
        }

        private void OnOpenChest()
        {
            var lootTable = ServerItemsRegistry.GetLootTableById(lootTableId);
            if (lootTable == null)
                return;

            var itemsToSpawn = new List<string>();
            foreach (var lootEntry in lootTable.lootItems)
            {
                if (UnityEngine.Random.Range(0, 100) < lootEntry.dropProbability)
                    itemsToSpawn.Add(lootEntry.id);
            }

            int amountOfGold = UnityEngine.Random.Range(
                lootTable.minGoldDropAmount,
                lootTable.maxGoldDropAmount + 1
            );

            SpawnGold(chestTopPosition, amountOfGold);
            foreach (string itemId in itemsToSpawn)
            {
                SpawnLootItem(chestTopPosition, itemId);
            }
            StartCoroutine(ChestResetCoroutine());
        }

        private IEnumerator ChestResetCoroutine()
        {
            logger.Log($"Chest {chestId} will reset in {chestResetTimeSeconds} seconds.");
            yield return new WaitForSeconds(chestResetTimeSeconds);
            chestStateStorage.SetChestState(false);
            logger.Log($"Chest {chestId} has reset.");
        }

        // Spawn Functions

        // TODO: this logic is taken from the Enemy spawn, we should move it to a helper
        // function to spawn loot items and gold since it can be used in multiple places (enemies, chests, quests, etc)
        private void SpawnGold(Vector3 position, int amount)
        {
            var lootItemPrefab = prefabProvider?.LootItemPrefab;
            if (lootItemPrefab != null)
            {
                GameObject lootInstance = resolverContainer.Resolver.Instantiate(
                    lootItemPrefab,
                    position,
                    Quaternion.identity
                );

                if (lootInstance != null)
                {
                    var stateStorage = lootInstance.GetComponent<LootItemStateStorage>();
                    if (stateStorage != null)
                    {
                        stateStorage.SetItemId("");
                        stateStorage.SetGoldAmount(amount);
                    }
                    NetworkServer.Spawn(lootInstance);
                }
            }
            else
            {
                logger.Log("[Chest] LootItem prefab not assigned on Chest!", this);
            }
        }

        private void SpawnLootItem(Vector3 position, string itemId)
        {
            var lootItemPrefab = prefabProvider?.LootItemPrefab;
            if (lootItemPrefab != null)
            {
                GameObject lootInstance = resolverContainer.Resolver.Instantiate(
                    lootItemPrefab,
                    position,
                    Quaternion.identity
                );

                if (lootInstance != null)
                {
                    var stateStorage = lootInstance.GetComponent<LootItemStateStorage>();
                    if (stateStorage != null)
                    {
                        stateStorage.SetItemId(itemId);
                        stateStorage.SetGoldAmount(0);
                    }
                    NetworkServer.Spawn(lootInstance);
                }
            }
            else
            {
                logger.Log("[Chest] LootItem prefab not assigned on ChestInteractSystem!", this);
            }
        }

        private void GetChestTopPosition()
        {
            var chestCollider = transform.parent.GetComponent<BoxCollider>();
            chestTopPosition =
                chestCollider != null
                    ? new Vector3(
                        chestCollider.bounds.center.x,
                        chestCollider.bounds.max.y + chestTopOffset,
                        chestCollider.bounds.center.z
                    )
                    : transform.parent.position + Vector3.up;
        }

        // TODO: move this to a utility class since it's used in ShopInteractSystem as well
        private int? GetPlayerConnectionId(uint playerNetId)
        {
            if (
                worldMonitor.Entities.TryGet(playerNetId, out var entity)
                && entity.ConnectionId.HasValue
            )
            {
                return entity.ConnectionId.Value;
            }
            return null;
        }
    }
}
