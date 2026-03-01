using FTR.Core.Common.Loaders;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters;

public class ServerCharacterLinker : IScriptLinker
{
    private readonly WorldMonitor world;

    public ServerCharacterLinker(WorldMonitor world)
    {
        this.world = world;
    }

    public void LinkDomainScripts(GameObject gameObject)
    {
        // Add Serverside components
        var serverCommandHandler = gameObject.AddComponent<ServerCommandHandler>();
        // TODO: move this to a Server character prefab and inject it here instead of creating it on the fly
        var movementSystem = gameObject.AddComponent<MovementSystem>();

        // Get from common character components
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var rb = gameObject.GetComponent<Rigidbody>();

        // Initialize components
        movementSystem.Initialize(rb, stateStorage);
        serverCommandHandler.Initialize(movementSystem);

        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var netID = gameObject.GetComponent<NetworkIdentity>().netId;
        RegisterEntity(netID, networkAdapter, serverCommandHandler);

        Debug.Log($"Linked domain scripts for character with netID {netID}");
    }

    public void RegisterEntity(
        uint netID,
        NetworkAdapter networkAdapter,
        ServerCommandHandler serverCommandHandler
    )
    {
        var entity = new ServerEntity(netID, networkAdapter, serverCommandHandler);

        world.Entities.Register(netID, entity);
    }
}
