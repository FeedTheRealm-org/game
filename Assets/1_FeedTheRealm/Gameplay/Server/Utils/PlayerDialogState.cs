using FTR.Core.Common.Interactions;
using FTR.Core.Server.Enums;

namespace FTR.Gameplay.Server.Utils
{
    public class PlayerDialogState
    {
        /// <summary>
        /// Index into NPCData.dialogProgression.
        /// Persists across interactions until the slot's quest is completed.
        /// </summary>
        public int ProgressionIndex;

        /// <summary>Current message index within the active dialog. Reset on each new Interact.</summary>
        public int MessageIndex;

        /// <summary>Current phase of the dialog flow for this player.</summary>
        public PlayerPhase Phase;

        /// <summary>
        /// When a quest is accepted and the slot has an onQuestAcceptedDialogId, this is
        /// cached here so ContinueInteraction can transition to it after the current dialog ends.
        /// Cleared when a quest completes and progression advances.
        /// </summary>
        public string OnAcceptedDialogId;

        /// <summary>The active interactor for this session (used by the inactivity timer).</summary>
        public IInteractor Interactor;
    }
}
