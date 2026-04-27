using FTR.Core.Common.Loaders;
using UnityEngine;

namespace FTR.Gameplay.Common.Linkers;

public abstract class LootItemLinker : IScriptLinker
{
    public abstract void Link(GameObject gameObject);
}
