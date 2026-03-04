using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Loaders;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Characters
{
    public class CharacterNetworkInitializer : NetworkBehaviour
    {
        [SerializeField]
        InitiatePlayerEvent initiatePlayerEvent;

        /// <summary>
        /// Called on the client when this is the local player.
        /// Client-side player spawns from network, so we need to inject dependencies first.
        /// </summary>
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Debug.Log(
                "CharacterInitializer: OnStartLocalPlayer - Injecting and initializing client-side local player"
            );

            var containerScope = FindFirstObjectByType<ClientWorldInitiator>();
            if (containerScope != null)
            {
                Debug.Log("START Injecting local player gameobject to container");
                containerScope.Container.InjectGameObject(gameObject);
                Debug.Log("DONE Injected local player gameobject to container!");
            }
            else
            {
                Debug.LogError("CharacterInitializer: No ClientWorldInitiator found in scene!");
            }
            gameObject.name = $"Player-{gameObject.GetComponent<NetworkIdentity>().netId}";
            initiatePlayerEvent.Raise();
        }
    }
}
