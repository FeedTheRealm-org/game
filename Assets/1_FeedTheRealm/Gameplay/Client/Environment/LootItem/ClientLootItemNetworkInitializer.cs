using FTR.Core.Common.Config;
using FTR.Core.Common.Scopes;
using FTR.Gameplay.Client.EntryPoints.Scopes;
using FTR.Gameplay.Common.Linkers;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Environment.LootItem
{
    public class ClientLootItemNetworkInitializer : NetworkBehaviour
    {
        [SerializeField]
        private Config config;

        /// <summary>
        /// Called on the client when any loot item (local or remote) is spawned.
        /// Injects dependencies via VContainer and initializes client-side scripts for
        /// every spawned character, not just the local player.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log(
                $"ClientLootItemNetworkInitializer: OnStartClient - Injecting {gameObject.name}"
            );
            InjectDependencies();
        }

        private void InjectDependencies()
        {
            var clientWorldInitiator = FindFirstObjectByType<ClientWorldInitiator>();
            if (config.RuntimeRole != RuntimeRole.Client)
                return;

            clientWorldInitiator.Container.InjectGameObject(gameObject);
            var identity = netIdentity;
            if (identity != null)
                gameObject.name = $"LootItem-{identity.netId}";
            else
                Debug.LogWarning(
                    $"ClientLootItemNetworkInitializer: No NetworkIdentity found on '{gameObject.name}'."
                );

            GetComponent<GameObjectLinker>()?.Initialize();
        }
    }
}
