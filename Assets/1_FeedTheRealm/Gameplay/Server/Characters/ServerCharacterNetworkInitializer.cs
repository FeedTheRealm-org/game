using FTR.Gameplay.Common.NetworkEntities.Characters;
using Mirror;
using UnityEngine;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Characters
{
    public class ServerCharacterNetworkInitializer : NetworkBehaviour
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
                $"ServerCharacterNetworkInitializer: OnStartServer - Injecting {gameObject.name}"
            );

            var serverWorldInitiator = FindFirstObjectByType<ServerWorldInitiator>();
            serverWorldInitiator.Container.InjectGameObject(gameObject);

            var identity = netIdentity;
            if (identity != null)
            {
                gameObject.name = $"Player-{identity.netId}";
            }
            else
            {
                Debug.LogWarning(
                    $"ServerCharacterNetworkInitializer: No NetworkIdentity found on '{gameObject.name}'."
                );
            }

            GetComponent<CharacterInitializer>()?.Initialize();
        }
    }
}
