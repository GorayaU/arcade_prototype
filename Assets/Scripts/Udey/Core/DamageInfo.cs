using UnityEngine;

namespace Udey.Core
{
    /// What kind of hit this was. Used by the ammo drop system and by armour/shields.
    public enum DamageType
    {
        Bullet,
        Melee,
        Explosion,
        Environment
    }
    
    /// Everything a damage event needs to carry. Passed around by value — it's a small readonly
    /// struct, so it never allocates.
    public readonly struct DamageInfo
    {
        /// Damage before any shields or resistances are applied.
        public readonly float Amount;

        public readonly DamageType Type;

        /// World position of the impact. Used for hit effects.
        public readonly Vector3 HitPoint;

        /// The direction the attack was TRAVELLING in. A frontal shield flips this to
        /// work out which way the hit came from.
        public readonly Vector3 Direction;

        /// Whoever dealt the damage. May be null for environmental damage.
        public readonly GameObject Source;

        public DamageInfo(float amount, DamageType type, Vector3 hitPoint, Vector3 direction, GameObject source)
        {
            Amount = amount;
            Type = type;
            HitPoint = hitPoint;
            Direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.forward;
            Source = source;
        }

        /// Shorthand for damage where the exact impact point doesn't matter.
        public static DamageInfo Simple(float amount, GameObject source, DamageType type = DamageType.Environment)
        {
            Vector3 position = source != null ? source.transform.position : Vector3.zero;
            Vector3 forward = source != null ? source.transform.forward : Vector3.forward;
            return new DamageInfo(amount, type, position, forward, source);
        }
    }
}