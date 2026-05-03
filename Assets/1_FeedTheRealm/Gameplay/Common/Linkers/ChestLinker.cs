using FTR.Core.Common.Loaders;
using UnityEngine;

namespace FTR.Gameplay.Common.Linkers;

public abstract class ChestLinker : IScriptLinker
{
    public abstract void Link(GameObject gameObject);
}
