using FTR.Core.Server;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Linkers;

public class ServerPassiveNpcLinker : PassiveNpcLinker
{
    private ServerCharacterLinker characterLinker;
    private IObjectResolver resolver;
    private WorldMonitor worldMonitor;

    public ServerPassiveNpcLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver
    )
    {
        this.worldMonitor = world;
        this.characterLinker = new ServerCharacterLinker(world, prefabProvider, resolver);
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        gameObject.name = $"PassiveNPC";

        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var rb = gameObject.GetComponent<Rigidbody>();

        var serverComponents = characterLinker.Link(gameObject, netId);

        var serverCommandHandler = serverComponents.GetComponent<ServerCommandHandler>();
        var movementSystem = serverComponents.GetComponent<MovementSystem>();
        var dashSystem = serverComponents.GetComponent<DashSystem>();
        var useSystem = serverComponents.GetComponent<UseSystem>();
        var interactSystem = serverComponents.GetComponent<PlayerInteractSystem>();

        var npcInteract = gameObject.AddComponent<NpcInteractSystem>();

        var logger = resolver.Resolve<Logging.Logger>();
        var npcDialogRegistry = resolver.Resolve<NpcDialogRegistry>();
        npcInteract.Initialize(logger, npcDialogRegistry, worldMonitor);

        serverCommandHandler.Initialize(movementSystem, dashSystem, useSystem, interactSystem);
        characterLinker.RegisterEntity(netId, networkAdapter, serverCommandHandler);
    }
}
