using System.Collections;
using System.Runtime.InteropServices;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Scopes;
using FTR.Core.Common.Systems.Status;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// DropItem represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class DropItemSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private ServerConfig config;
        private uint netId;

        [SerializeField]
        private ObjectResolverContainer resolverContainer;

        [Header("Spawn settings")]
        [SerializeField]
        private GameObject thingPrefab;

        public void Initialize(uint netId)
        {
            this.netId = netId;
        }

        public void OnDropItem(string itemId)
        {
            Vector3 point = transform.position;
            GameObject Thing = resolverContainer.Resolver?.Instantiate(
                thingPrefab,
                point,
                Quaternion.identity
            );
            Thing.name = $"Dropped-Item-{itemId}";
            NetworkServer.Spawn(Thing);
        }

        public void GameTick(float dt) { }
    }
}
