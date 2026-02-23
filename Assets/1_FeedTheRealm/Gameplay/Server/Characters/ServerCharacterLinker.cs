using FTR.Core.Common.Loaders;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters;

public class ServerCharacterLinker : IScriptLinker
{
    public void LinkDomainScripts(GameObject gameObject)
    {
        // Add Serverside components
        var serverCommandHandler = gameObject.AddComponent<ServerCommandHandler>();
        var movementSystem = gameObject.AddComponent<MovementSystem>();

        // Get from common character components
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var rb = gameObject.GetComponent<Rigidbody>();

        // Initialize components
        movementSystem.Initialize(rb, stateStorage);
        serverCommandHandler.Initialize(movementSystem);
    }
}
