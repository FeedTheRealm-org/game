using FTR.Core.Common.Config;
using FTR.Core.Common.Scopes;
using FTR.Gameplay.Client.EntryPoints.Scopes;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Chest;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Environment.Shop
{
    public class ClientChestNetworkInitializer : NetworkBehaviour
    {
        [SerializeField]
        private Config config;

        /// <summary>
        /// Called on the client when any chest (local or remote) is spawned.
        /// Injects dependencies via VContainer and initializes client-side scripts for
        /// every spawned character, not just the local player.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (config.RuntimeRole != RuntimeRole.Client)
                return;

            Debug.Log(
                $"ClientChestNetworkInitializer: OnStartClient - Injecting {gameObject.name}"
            );
            var clientWorldInitiator = FindFirstObjectByType<ClientWorldInitiator>();
            clientWorldInitiator.Container.InjectGameObject(gameObject);
            GetComponent<GameObjectLinker>().Initialize();
        }
    }
}
