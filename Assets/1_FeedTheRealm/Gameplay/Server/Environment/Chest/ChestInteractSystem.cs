using System;
using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Portal;
using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Registry;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Environment.Chest
{
    /// <summary>
    /// Server-side chest system. Handles teleportation logic and validation.
    /// </summary>
    public class ChestInteractSystem : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private int chestResetTimeSeconds = 60;
        private WorldMonitor worldMonitor;
        private bool isOpen = false;
        private string chestId;
        private List<string> itemIds = new();

        public bool CanInteract(IInteractor interactor)
        {
            return true;
        }

        public void StopInteraction(IInteractor interactor)
        {
            return;
        }

        public void Initialize(WorldMonitor worldMonitor)
        {
            this.worldMonitor = worldMonitor;
        }

        public string Interact(IInteractor interactor)
        {
            OpenChest(interactor.NetId);
            return chestId;
        }

        private void OpenChest(uint playerNetId)
        {
            if (isOpen)
            {
                logger.Log($"Chest {chestId} is already open. Ignoring interaction.");
                return;
            }
            logger.Log($"Player {playerNetId} opened chest {chestId}.");
            isOpen = true;
            StartCoroutine(ChestResetCoroutine());
        }

        private IEnumerator ChestResetCoroutine()
        {
            logger.Log($"Chest {chestId} will reset in {chestResetTimeSeconds} seconds.");
            yield return new WaitForSeconds(chestResetTimeSeconds);
            isOpen = false;
            logger.Log($"Chest {chestId} has reset.");
        }

        private GameObject GetPlayerGameObject(uint playerNetId)
        {
            if (NetworkServer.spawned.TryGetValue(playerNetId, out NetworkIdentity identity))
            {
                return identity.gameObject;
            }
            return null;
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
