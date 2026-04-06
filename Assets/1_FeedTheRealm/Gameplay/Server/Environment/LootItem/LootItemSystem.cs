using System.Collections;
using System.Runtime.InteropServices;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Systems.Status;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Environment.LootItem
{
    /// <summary>
    /// LootItem represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class LootItemSystem : MonoBehaviour, IGameTickable, IGroundable
    {
        [Inject]
        private WorldMonitor worldMonitor;

        [SerializeField]
        private GameTickEvent gameTickEvent;

        [SerializeField]
        private ServerConfig config;

        // Injected dependencies
        private Rigidbody rb;
        private uint netId;
        private LootItemStateStorage stateStorage;

        // Configurable parameters
        private ushort maxInitialForce = 5; // default max force applied to the item when spawned

        private bool isGrounded;
        public bool IsGrounded
        {
            get => isGrounded;
            set
            {
                isGrounded = value;
                if (value)
                    GroundItem();
            }
        }

        public void Initialize(Rigidbody rb, uint netId, LootItemStateStorage stateStorage)
        {
            this.rb = rb;
            this.netId = netId;
            this.stateStorage = stateStorage;
            maxInitialForce = config.MaxInitialForce > 0 ? config.MaxInitialForce : maxInitialForce;
            SpawnItemWithForce();
        }

        private void SpawnItemWithForce()
        {
            Vector3 randomInitialForce = new(
                Random.Range(-maxInitialForce, maxInitialForce),
                Random.Range(0, maxInitialForce),
                Random.Range(-maxInitialForce, maxInitialForce)
            );
            rb.AddForce(randomInitialForce, ForceMode.VelocityChange);

            var initialForceEvent = CreateInitialForceEvent(randomInitialForce, transform.position);

            worldMonitor.Events.Enqueue(initialForceEvent);
        }

        private InitialForceEvent CreateInitialForceEvent(Vector3 force, Vector3 position)
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
            return new InitialForceEvent(netId, initialForceEventContent);
        }

        public void GroundItem()
        {
            stateStorage.CorrectPosition(rb.position);
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        public void GameTick(float dt) { }
    }
}
