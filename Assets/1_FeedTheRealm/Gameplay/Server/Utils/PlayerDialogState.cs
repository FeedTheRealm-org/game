using FTR.Core.Common.Interactions;
using FTR.Core.Server.Enums;

namespace FTR.Gameplay.Server.Utils
{
    public class PlayerDialogState
    {
        /// <summary>Current message index within the active dialog. Reset on each new Interact.</summary>
        public int MessageIndex;

        /// <summary>Current phase of the dialog flow for this player.</summary>
        public PlayerPhase Phase;

        public string ActiveDialogId;

        /// <summary>The active interactor for this session (used by the inactivity timer).</summary>
        public IInteractor Interactor;
    }
}
