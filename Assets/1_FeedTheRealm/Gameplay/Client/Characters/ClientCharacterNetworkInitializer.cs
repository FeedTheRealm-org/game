using FTR.Core.Common.Config;
using FTR.Gameplay.Client.EntryPoints.Scopes;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.Linkers;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Characters
{
    public class ClientCharacterNetworkInitializer : NetworkBehaviour
    {
        [SerializeField]
        private Config config;

        /// <summary>
        /// Called on the client when any character (local or remote) is spawned.
        /// Injects dependencies via VContainer and initializes client-side scripts for
        /// every spawned character, not just the local player.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();

            var clientWorldInitiator = FindFirstObjectByType<ClientWorldInitiator>();
            if (config.RuntimeRole != RuntimeRole.Client)
                return;

            Debug.Log(
                $"ClientCharacterNetworkInitializer: OnStartClient - Injecting {gameObject.name}"
            );

            clientWorldInitiator.Container.InjectGameObject(gameObject);

            GetComponent<GameObjectLinker>()?.Initialize();
        }
    }
}
