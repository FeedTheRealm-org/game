using System;

namespace FTR.Gameplay.Client.EntryPoints
{
    public interface IMainMenuController
    {
        event Action OnNavigateToWorld;
    }
}
