using FTR.Gameplay.Common.NetworkEntities.Characters;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Characters
{
    public class ClientCharacterNetworkInitializer : NetworkBehaviour
    {
        /// <summary>
        /// Called on the client when any character (local or remote) is spawned.
        /// Injects dependencies via VContainer and initializes client-side scripts for
        /// every spawned character, not just the local player.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log(
                $"ClientCharacterNetworkInitializer: OnStartClient - Injecting {gameObject.name}"
            );

            var clientWorldInitiator = FindFirstObjectByType<ClientWorldInitiator>();
            clientWorldInitiator.Container.InjectGameObject(gameObject);

            GetComponent<CharacterInitializer>()?.Initialize();
        }
    }
}
