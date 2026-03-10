using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Environment.LootItem
{
    public class ServerLootItemLinker : IScriptLinker
    {
        private readonly WorldMonitor world;
        private readonly ServerPrefabProvider prefabProvider;
        private readonly IObjectResolver resolver;

        public ServerLootItemLinker(
            WorldMonitor world,
            ServerPrefabProvider prefabProvider,
            IObjectResolver resolver
        )
        {
            this.world = world;
            this.prefabProvider = prefabProvider;
            this.resolver = resolver;
        }

        public void LinkDomainScripts(GameObject gameObject, bool linkNPC)
        {
            var rb = gameObject.GetComponent<Rigidbody>();
            var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
            var netId = gameObject.GetComponent<NetworkIdentity>().netId;

            // Add server-side components
            var serverComponents = Object.Instantiate(
                prefabProvider.ServerCharacterComponents,
                gameObject.transform
            );

            resolver.InjectGameObject(serverComponents);

            var lootItemSystem = serverComponents.GetComponent<LootItemSystem>();
            var lootItemController = serverComponents.GetComponent<LootItemController>();

            // Initialize components
            lootItemController.Initialize(netId);
            lootItemSystem.Initialize(rb, lootItemController, netId);

            RegisterEntity(netId, networkAdapter);

            Debug.Log($"Linked domain scripts for character with netID {netId}");
        }

        public void RegisterEntity(uint netID, NetworkAdapter networkAdapter)
        {
            var entity = new ServerEntity(netID, networkAdapter, null);
            world.Entities.Register(netID, entity);
        }
    }
}
