using UnityEngine;

namespace FTR.Gameplay.Server.Utils.UseEquipment
{
    /// <summary>
    /// Tracks active temporary stat modifiers for a single entity.
    /// </summary>
    public sealed class StatModifierBag
    {
        // ── Flat damage bonus ─────────────────────────────────────────────────
        private int _flatDamageBonus;
        private float _damageExpiry;

        public int FlatDamageBonus => Time.time < _damageExpiry ? _flatDamageBonus : 0;

        /// <summary>
        /// Apply a flat damage bonus for <paramref name="duration"/> seconds.
        /// Calling again while active overwrites the previous buff.
        /// </summary>
        public void ApplyFlatDamage(int bonus, float duration)
        {
            _flatDamageBonus = bonus;
            _damageExpiry = Time.time + duration;
        }

        // ── Future modifiers ──────────────────────────────────────────────────
        // Follow the same pattern: a private backing value + expiry float,
        // a public read property that checks Time.time, and an Apply method.
        //
        // Example:
        // private float _defenseMultiplier;
        // private float _defenseExpiry;
        // public float DefenseMultiplier => Time.time < _defenseExpiry ? _defenseMultiplier : 1f;
        // public void ApplyDefense(float multiplier, float duration) { ... }
    }
}
