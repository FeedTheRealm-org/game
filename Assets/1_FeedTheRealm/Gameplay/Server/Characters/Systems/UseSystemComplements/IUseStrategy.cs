using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Utils.UseEquipment;
using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements
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
        /// Returns the cooldown in seconds that should be applied after execution.
        /// UseSystem applies it to the appropriate tracker (slot or consumable-specific).
        /// </summary>
        float GetCooldown(UseContext ctx);

        /// <summary>
        /// Optional per-strategy cooldown guard called by UseSystem before Execute.
        /// Return false (and populate <paramref name="remaining"/>) to block the action.
        /// Default implementation always allows execution.
        /// </summary>
        bool CanExecute(UseContext ctx, SlotCooldownTracker cooldowns, out float remaining)
        {
            remaining = 0f;
            return true;
        }

        /// <summary>
        /// Called by UseSystem after CanExecute passes, before Execute,
        /// so each strategy can record its own cooldown entry.
        /// </summary>
        void RecordCooldown(UseContext ctx, SlotCooldownTracker cooldowns, int activeSlot) { }
    }
}
