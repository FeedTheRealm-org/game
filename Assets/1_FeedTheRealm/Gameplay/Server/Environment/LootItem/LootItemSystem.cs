using System.Collections;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Server.Environment.Items
{
    /// <summary>
    /// LootItem represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class LootItemSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private GameTickEvent gameTickEvent;

        [SerializeField]
        private ServerConfig config;
        private Rigidbody rb;
        private LootItemController controller;
        private NetworkAdapter networkAdapter;
        private NetworkIdentity networkIdentity;
        private uint despawnTime = 10; // default despawn time in seconds
        private ushort maxInitialForce = 5; // default max force applied to the item when spawned

        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            controller = GetComponent<LootItemController>();
            networkAdapter = GetComponent<NetworkAdapter>();
            networkIdentity = GetComponent<NetworkIdentity>();

            despawnTime = config.ItemDespawnTime > 0 ? config.ItemDespawnTime : despawnTime;
            maxInitialForce = config.MaxInitialForce > 0 ? config.MaxInitialForce : maxInitialForce;
            SpawnItemWithForce();
            StartCoroutine(DespawnObject());
        }

        private void SpawnItemWithForce()
        {
            Vector3 randomInitialForce = new Vector3(
                Random.Range(-maxInitialForce, maxInitialForce),
                Random.Range(0, maxInitialForce),
                Random.Range(-maxInitialForce, maxInitialForce)
            );
            rb.AddForce(randomInitialForce, ForceMode.Impulse);

            var initialForceEventDTO = SerializeInitialForceEvent(
                randomInitialForce,
                transform.position
            );

            networkAdapter.DispatchEvent(initialForceEventDTO);
        }

        private ServerEventDTO SerializeInitialForceEvent(Vector3 force, Vector3 position)
        {
            var initialForceEventContent = new InitialForceEventContent
            {
                InitialPosition = new ProtoVector3
                {
                    X = position.x,
                    Y = position.y,
                    Z = position.z,
                },
                Force = new ProtoVector3
                {
                    X = force.x,
                    Y = force.y,
                    Z = force.z,
                },
            };
            var initialForceEvent = new InitialForceEvent(
                networkIdentity.netId,
                initialForceEventContent
            );
            return initialForceEvent.ToDTO();
        }

        private IEnumerator DespawnObject()
        {
            yield return new WaitForSeconds(despawnTime);
            if (!controller.IsPickedUp)
                Destroy(gameObject);
        }

        public void GameTick(float dt) { }
    }
}
