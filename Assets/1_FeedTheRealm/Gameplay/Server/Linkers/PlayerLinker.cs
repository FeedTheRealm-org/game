using FTR.Core.Server;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Linkers;

public class ServerPlayerLinker : PlayerLinker
{
    private ServerCharacterLinker characterLinker;

    public ServerPlayerLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver
    )
    {
        this.characterLinker = new ServerCharacterLinker(world, prefabProvider, resolver);
    }

    //TODO: add inventory and fastslot initialization here,
    // but need to separate command handlers (NPC/PLAYER/ENEMY)
    public override void Link(GameObject gameObject)
    {
        var serverComponents = characterLinker.Link(gameObject);
        gameObject.name = $"Player";

        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var rb = gameObject.GetComponent<Rigidbody>();

        var serverCommandHandler = serverComponents.GetComponent<ServerCommandHandler>();
        var healthSystem = serverComponents.GetComponent<HealthSystem>();
        var respawnSystem = serverComponents.GetComponent<RespawnSystem>();
        var persistenceSystem = serverComponents.GetComponent<PersistenceSystem>();
        var movementSystem = serverComponents.GetComponent<MovementSystem>();
        var inventorySystem = serverComponents.GetComponent<InventorySystem>();
        respawnSystem.Initialize(netId, networkAdapter, serverCommandHandler, rb, healthSystem);
        persistenceSystem.Initialize(movementSystem, inventorySystem);
    }
}
