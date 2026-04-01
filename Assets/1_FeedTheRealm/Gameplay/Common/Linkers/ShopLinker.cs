using FTR.Core.Common.Loaders;
using UnityEngine;

namespace FTR.Gameplay.Common.Linkers;

public abstract class ShopLinker : IScriptLinker
{
    public abstract void Link(GameObject gameObject);
}
