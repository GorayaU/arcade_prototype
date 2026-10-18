namespace Udey.Core
{
    /// Anything that can be shot or hit. Weapons and enemy attacks look for this with
    /// GetComponentInParent, so the collider doesn't have to be on the same object as the Health.
    public interface IDamageable
    {
        bool IsDead { get; }

        void TakeDamage(DamageInfo info);
    }
}
