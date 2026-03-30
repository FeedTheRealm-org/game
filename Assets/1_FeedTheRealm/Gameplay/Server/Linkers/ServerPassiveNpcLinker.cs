using FTR.Core.Server;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Linkers;

public class ServerPassiveNpcLinker : PassiveNpcLinker
{
    private readonly ServerCharacterLinker characterLinker;
    private readonly ServerPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;
    private readonly WorldMonitor worldMonitor;

    public ServerPassiveNpcLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver
    )
    {
        this.worldMonitor = world;
        this.characterLinker = new ServerCharacterLinker(world, prefabProvider, resolver);
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        gameObject.name = "PassiveNPC";

        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

        var systems = characterLinker.Link(gameObject, netId);

        var npcComponents = resolver.Instantiate(
            prefabProvider.ServerNpcComponents,
            gameObject.transform
        );
        var npcCommandHandler = npcComponents.GetComponent<NpcCommandHandler>();

        var npcInteract = gameObject.AddComponent<NpcInteractSystem>();
        var logger = resolver.Resolve<Logging.Logger>();
        var npcDialogRegistry = resolver.Resolve<NpcDialogRegistry>();

        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        npcInteract.Initialize(
            logger,
            npcDialogRegistry,
            worldMonitor,
            netId,
            stateStorage.CharacterId
        );

        var aiNavigationSystem = npcComponents.AddComponent<AINavigationSystem>();

        resolver.Inject(aiNavigationSystem);

        systems.Health.Initialize(netId, stateStorage, true);
        npcCommandHandler.Initialize(systems.Movement);
        aiNavigationSystem.Initialize(netId, worldMonitor, stateStorage);

        characterLinker.RegisterEntity(netId, networkAdapter, npcCommandHandler);
    }
}
