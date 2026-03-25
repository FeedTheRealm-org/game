using FTR.Core.Server;
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
    private readonly WorldMonitor world;

    public ServerPassiveNpcLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver
    )
    {
        this.world = world;
        this.characterLinker = new ServerCharacterLinker(world, prefabProvider, resolver);
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
        var interactSystem = serverComponents.GetComponent<InteractSystem>();
        var aiNavigationSystem = serverComponents.AddComponent<AINavigationSystem>();

        serverCommandHandler.Initialize(movementSystem, dashSystem, useSystem, interactSystem);

        aiNavigationSystem.Initialize(netId, world);

        characterLinker.RegisterEntity(netId, networkAdapter, serverCommandHandler);
    }
}
