using System.Collections.Generic;
using FTR.Core.Server.Enums;
using FTR.Gameplay.Common.Environment.Dialogs;
using UnityEngine;

namespace FTR.Gameplay.Server.Utils
{
    /// <summary>
    /// Manages per-player dialog progression state for a single NPC.
    /// Owns the <see cref="PlayerDialogState"/> dictionary and all interactions with
    /// <see cref="NpcDialogRegistry"/> so <see cref="NpcInteractSystem"/> only deals with
    /// the interaction lifecycle and event dispatching.
    /// </summary>
    public class NpcDialogProgressionTracker
    {
        private readonly string npcId;
        private readonly NpcDialogRegistry registry;
        private readonly Logging.Logger logger;

        private readonly Dictionary<uint, PlayerDialogState> _states =
            new Dictionary<uint, PlayerDialogState>();

        public NpcDialogProgressionTracker(
            string npcId,
            NpcDialogRegistry registry,
            Logging.Logger logger
        )
        {
            this.npcId = npcId;
            this.registry = registry;
            this.logger = logger;
        }

        public PlayerDialogState GetOrCreate(uint playerNetId)
        {
            if (!_states.TryGetValue(playerNetId, out var state))
            {
                state = new PlayerDialogState { ProgressionIndex = 0 };
                _states[playerNetId] = state;
            }
            return state;
        }

        public bool TryGet(uint playerNetId, out PlayerDialogState state) =>
            _states.TryGetValue(playerNetId, out state);

        public void OnQuestDecided(uint playerNetId)
        {
            if (!_states.TryGetValue(playerNetId, out var state))
                return;

            if (state.Phase != PlayerPhase.WaitingForQuestDecision)
                return;

            state.Phase = PlayerPhase.Normal;
        }

        /// <summary>
        /// Caches the onQuestAccepted dialogId so can be
        /// transitioned to it once the current dialog's messages are exhausted.
        /// </summary>
        public void OnQuestAccepted(uint playerNetId)
        {
            if (!_states.TryGetValue(playerNetId, out var state))
                return;

            string onAcceptedId = registry.GetOnQuestAcceptedDialogId(
                npcId,
                state.ProgressionIndex
            );

            if (!string.IsNullOrEmpty(onAcceptedId))
            {
                state.OnAcceptedDialogId = onAcceptedId;
            }
        }

        /// <summary>
        /// Advances (or keeps) the progression slot for a player when a quest is completed.
        /// Filters out completions that don't belong to this NPC's current slot.
        /// </summary>
        public void OnQuestCompleted(uint playerNetId, string questId)
        {
            if (!_states.TryGetValue(playerNetId, out var state))
                return;

            if (GetQuestIdForSlot(state) != questId)
                return;

            // Reset session-level fields regardless of repeatability
            state.Phase = PlayerPhase.Normal;
            state.OnAcceptedDialogId = null;
            state.MessageIndex = 0;

            if (registry.IsRepeatableAt(npcId, state.ProgressionIndex))
            {
                return;
            }

            int next = state.ProgressionIndex + 1;
            int total = registry.GetProgressionCount(npcId);

            if (next < total)
                state.ProgressionIndex = next;
        }

        public string GetActiveDialogId(PlayerDialogState state)
        {
            if (
                state.Phase == PlayerPhase.InOnQuestAcceptedDialog
                && !string.IsNullOrEmpty(state.OnAcceptedDialogId)
            )
                return state.OnAcceptedDialogId;

            return registry.GetDialogId(npcId, state.ProgressionIndex);
        }

        public int GetCurrentMessageCount(PlayerDialogState state)
        {
            if (
                state.Phase == PlayerPhase.InOnQuestAcceptedDialog
                && !string.IsNullOrEmpty(state.OnAcceptedDialogId)
            )
                return GetMessageCountForDialog(state.OnAcceptedDialogId);

            return registry.GetMessageCount(npcId, state.ProgressionIndex);
        }

        public string GetQuestIdForCurrentMessage(PlayerDialogState state)
        {
            if (state.Phase == PlayerPhase.InOnQuestAcceptedDialog)
                return string.Empty;

            return registry.GetQuestIdAt(npcId, state.ProgressionIndex, state.MessageIndex);
        }

        /// <summary>
        /// Returns the questId assigned anywhere in the current progression slot.
        /// Used to match against quest-completion events.
        /// </summary>
        public string GetQuestIdForSlot(PlayerDialogState state)
        {
            int count = registry.GetMessageCount(npcId, state.ProgressionIndex);
            for (int i = 0; i < count; i++)
            {
                var qId = registry.GetQuestIdAt(npcId, state.ProgressionIndex, i);
                if (!string.IsNullOrEmpty(qId))
                    return qId;
            }
            return string.Empty;
        }

        public int GetOnQuestAcceptedMessageCount(PlayerDialogState state) =>
            !string.IsNullOrEmpty(state.OnAcceptedDialogId)
                ? GetMessageCountForDialog(state.OnAcceptedDialogId)
                : 0;

        private int GetMessageCountForDialog(string dialogId)
        {
            if (registry.TryGetMessagesByDialogId(dialogId, out var msgs))
                return msgs.Count;
            return 0;
        }
    }
}
