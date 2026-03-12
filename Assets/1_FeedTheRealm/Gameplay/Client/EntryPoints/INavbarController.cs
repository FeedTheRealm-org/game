using UnityEngine;

/// <summary>
/// Allows to inject menu profile instance without circular dependencies.
/// </summary>
public interface INavbarController
{
    void SetProfileMenuInstance(GameObject instance);
}
