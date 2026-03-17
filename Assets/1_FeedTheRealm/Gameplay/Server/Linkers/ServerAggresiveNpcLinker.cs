using FTR.Core.Server;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Linkers;

public class ServerAggresiveNpcLinker : AggresiveNpcLinker
{
    private ServerCharacterLinker characterLinker;

    public ServerAggresiveNpcLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver
    )
    {
        this.characterLinker = new ServerCharacterLinker(world, prefabProvider, resolver);
    }

    public override void Link(GameObject gameObject)
    {
        gameObject.name = $"AgressiveNPC";

        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var rb = gameObject.GetComponent<Rigidbody>();

        var serverComponents = characterLinker.Link(gameObject, netId);

        var serverCommandHandler = serverComponents.GetComponent<ServerCommandHandler>();
        var movementSystem = serverComponents.GetComponent<MovementSystem>();
        var dashSystem = serverComponents.GetComponent<DashSystem>();
        var useSystem = serverComponents.GetComponent<UseSystem>();
        var healthSystem = serverComponents.GetComponent<HealthSystem>();
        var groundCheckSystem = serverComponents.GetComponent<GroundCheckSystem>();
        var interactSystem = serverComponents.GetComponent<InteractSystem>();

        movementSystem.Initialize(rb, null);
        dashSystem.Initialize(netId, rb, null);
        useSystem.Initialize(netId, rb);
        groundCheckSystem.Initialize(gameObject.GetComponent<Collider>(), null);
        healthSystem.Initialize(netId, null);
        interactSystem.Initialize(netId, null);

        serverCommandHandler.Initialize(movementSystem, dashSystem, useSystem, interactSystem);

        characterLinker.RegisterEntity(netId, networkAdapter, serverCommandHandler);
    }
}
