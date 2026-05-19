using UnityEngine;

namespace FTR.Gameplay.Client.EntryPoints
{
    /// <summary>
    /// Allows to inject menu profile instance without circular dependencies.
    /// </summary>
    public interface INavbarController
    {
        void SetHomeMenuInstance(GameObject instance);
        void SetProfileMenuInstance(GameObject instance);
        void SetGemStoreInstance(GameObject instance);
        void SetNavBarSettingsInstance(GameObject instance);
    }
}
