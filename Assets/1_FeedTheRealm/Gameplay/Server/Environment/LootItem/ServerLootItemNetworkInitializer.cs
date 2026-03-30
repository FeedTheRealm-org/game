using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Server.EntryPoints.Scopes;
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
            var serverWorldInitiator = FindFirstObjectByType<ServerWorldInitiator>();
            serverWorldInitiator.Container.InjectGameObject(gameObject);
            GetComponent<GameObjectLinker>()?.Initialize();
        }
    }
}
