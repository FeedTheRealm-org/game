using UnityEngine;

namespace FTR.Core.Common.Loaders;

/// <summary>
/// Interface for linking client/server scripts respectively to the given GameObject.
/// Used to share the same prefab but attach different scripts at runtime.
/// </summary>
public interface IScriptLinker
{
    void LinkDomainScripts(GameObject gameObject);
}
