using System;
using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Scopes;
using FTR.Core.Server;
using FTR.Core.Server.Config;
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
        private int chestResetTimeSeconds = 60;

        private ChestStateStorage chestStateStorage;
        private ServerPrefabProvider prefabProvider;

        public string ChestId { get; set; }
        public string LootTableId { get; set; }
        public Vector3 chestTopPosition;

        public bool CanInteract(IInteractor interactor)
        {
            return true;
        }

        public void StopInteraction(IInteractor interactor)
        {
            return;
        }

        public void Initialize(ChestStateStorage chestStateStorage)
        {
            this.chestStateStorage = chestStateStorage;
            prefabProvider = resolverContainer.Resolver.Resolve<ServerPrefabProvider>();
            chestTopPosition = GetChestTopPosition();
        }

        public string Interact(IInteractor interactor)
        {
            OpenChest(interactor.NetId);
            return ChestId;
        }

        private void OpenChest(uint playerNetId)
        {
            if (chestStateStorage.IsOpen)
            {
                logger.Log($"Chest {ChestId} is already open. Ignoring interaction.");
                return;
            }
            logger.Log($"Player {playerNetId} opened chest {ChestId}.");
            chestStateStorage.SetChestState(true);
            OnOpenChest();
        }

        private void OnOpenChest()
        {
            var lootTable = ServerItemsRegistry.GetLootTableById(LootTableId);
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

            StartCoroutine(SpawnLootItemsCoroutine(itemsToSpawn, amountOfGold));
        }

        private IEnumerator SpawnLootItemsCoroutine(List<string> itemIds, int goldAmount)
        {
            SpawnGold(chestTopPosition, goldAmount);
            foreach (string itemId in itemIds)
            {
                SpawnLootItem(chestTopPosition, itemId);
                yield return new WaitForSeconds(serverConfig.ChestItemSpawnRateSeconds);
            }
            yield return ChestResetCoroutine();
        }

        private IEnumerator ChestResetCoroutine()
        {
            logger.Log($"Chest {ChestId} will reset in {chestResetTimeSeconds} seconds.");
            yield return new WaitForSeconds(chestResetTimeSeconds);
            chestStateStorage.SetChestState(false);
            logger.Log($"Chest {ChestId} has reset.");
        }

        // Helper Functions

        private Vector3 GetChestTopPosition()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return transform.position;

            Bounds combined = renderers[0].bounds;
            foreach (var r in renderers)
                combined.Encapsulate(r.bounds);

            return new Vector3(transform.position.x, combined.max.y, transform.position.z);
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

                    var rb = lootInstance.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        float angle = UnityEngine.Random.Range(30f, 60f);
                        float horizontalAngle = UnityEngine.Random.Range(0f, 360f);
                        Vector3 direction =
                            Quaternion.Euler(-angle, horizontalAngle, 0f) * Vector3.up;
                        float force = UnityEngine.Random.Range(3f, 6f);
                        rb.AddForce(direction * force, ForceMode.Impulse);
                    }

                    NetworkServer.Spawn(lootInstance);
                }
            }
            else
            {
                logger.Log("[Chest] LootItem prefab not assigned on Chest!", this);
            }
        }
    }
}
