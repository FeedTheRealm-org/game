using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using Mirror;
using UnityEngine;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Environment.LootItem
{
    public class ServerLootItemNetworkInitializer : NetworkBehaviour
    {
        /// <summary>
        /// Called on the server when any character (local or remote) is spawned.
        /// Injects dependencies via VContainer and initializes server-side scripts for
        /// every spawned character, not just the local player.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log(
                $"ServerLootItemNetworkInitializer: OnStartServer - Injecting {gameObject.name}"
            );

            var serverWorldInitiator = FindFirstObjectByType<ServerWorldInitiator>();
            serverWorldInitiator.Container.InjectGameObject(gameObject);
            GetComponent<LootItemInitializer>()?.Initialize();
        }
    }
}
