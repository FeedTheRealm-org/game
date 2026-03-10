using FTR.Core.Common.Loaders;
using UnityEngine;

namespace FTR.Gameplay.Common.Linkers;

public abstract class AggresiveNpcLinker : IScriptLinker
{
    public abstract void Link(GameObject gameObject);
}
