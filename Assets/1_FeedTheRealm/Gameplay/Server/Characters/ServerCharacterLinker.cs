using FTR.Core.Common.Loaders;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters;

public class ServerCharacterLinker : IScriptLinker
{
    public void LinkDomainScripts(GameObject gameObject)
    {
        gameObject.AddComponent<ServerCommandHandler>();
    }
}
