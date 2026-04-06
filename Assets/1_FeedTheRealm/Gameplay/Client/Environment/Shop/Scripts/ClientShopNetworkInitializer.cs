using FTR.Core.Common.Scopes;
using FTR.Gameplay.Client.EntryPoints.Scopes;
using FTR.Gameplay.Common.Linkers;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Environment.Shop
{
    public class ClientShopNetworkInitializer : NetworkBehaviour
    {
        /// <summary>
        /// Called on the client when any shop (local or remote) is spawned.
        /// Injects dependencies via VContainer and initializes client-side scripts for
        /// every spawned character, not just the local player.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log($"ClientShopNetworkInitializer: OnStartClient - Injecting {gameObject.name}");
            InjectDependencies();
        }

        private void InjectDependencies()
        {
            var clientWorldInitiator = FindFirstObjectByType<ClientWorldInitiator>();
            clientWorldInitiator.Container.InjectGameObject(gameObject);
            var identity = netIdentity;
            if (identity != null)
                gameObject.name = $"Shop-{identity.netId}";
            else
                Debug.LogWarning(
                    $"ClientShopNetworkInitializer: No NetworkIdentity found on '{gameObject.name}'."
                );

            GetComponent<GameObjectLinker>().Initialize();
        }
    }
}
