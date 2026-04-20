using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using UnityEngine;

namespace FTR.Gameplay.Server.Utils.UseEquipment
{
    // ── Strategy interface ────────────────────────────────────────────────────

    /// <summary>
    /// Defines how an equipped item behaves when the player presses Use.
    /// Add a new strategy to support a new item category without changing UseSystem.
    /// </summary>
    public interface IUseStrategy
    {
        /// <summary>Execute the use action (attack, consume, cast, etc.).</summary>
        void Execute(UseContext ctx);

        /// <summary>
        /// Cooldown in seconds applied after execution.
        /// UseSystem applies it to the appropriate tracker (slot or consumable-specific).
        /// </summary>
        float GetCooldown(UseContext ctx);

        /// <summary>
        /// Per-strategy cooldown guard called by UseSystem before Execute.
        /// Return false (and populate <paramref name="remaining"/>) to block the action.
        /// Default implementation always allows execution.
        /// </summary>
        bool CanExecute(UseContext ctx, SlotCooldownTracker cooldowns, out float remaining)
        {
            remaining = 0f;
            return true;
        }

        /// <summary>
        /// Called by UseSystem after CanExecute passes, before Execute.
        /// Each strategy records its own cooldown entry here.
        /// </summary>
        void RecordCooldown(UseContext ctx, SlotCooldownTracker cooldowns, int activeSlot) { }
    }
}
