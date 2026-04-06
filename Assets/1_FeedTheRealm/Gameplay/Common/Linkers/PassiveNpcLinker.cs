using FTR.Core.Common.Loaders;
using UnityEngine;

namespace FTR.Gameplay.Common.Linkers;

public abstract class PassiveNpcLinker : IScriptLinker
{
    public abstract void Link(GameObject gameObject);
}
