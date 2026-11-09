using System;
using UnityEngine;
using UnityEngine.Events;

namespace Udey.Core
{
    /// /// Shared health pool. The player and every enemy use this same component — only the
    /// numbers differ. Anything that wants to react to damage subscribes to the C# events;
    /// the UnityEvents are there so you can hook up audio/VFX in the inspector.

    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("Pool")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Death")]
        [Tooltip("Destroy this GameObject after it dies. Leave off for the player.")]
        [SerializeField] private bool destroyOnDeath = true;

        [Tooltip("Delay before destroying, so a death animation or ragdoll can play.")]
        [SerializeField] private float destroyDelay = 3f;

        [Header("Inspector hooks")]
        public UnityEvent onDamagedEvent;
        public UnityEvent onDiedEvent;

        // --- State ---

        public float Max => maxHealth;
        public float Current { get; private set; }
        public float Normalized => maxHealth <= 0f ? 0f : Current / maxHealth;
        public bool IsDead { get; private set; }

        /// (hit, damage actually dealt after modifiers)
        public event Action<DamageInfo, float> OnDamaged;

        /// (amount actually restored)
        public event Action<float> OnHealed;

        /// Fires once, with the killing blow. Always fires BEFORE the object is destroyed.
        public event Action<DamageInfo> OnDied;

        /// Catch-all for UI. Fires on damage, heal and reset.
        public event Action OnChanged;

        private IDamageModifier[] _modifiers;

        private void Awake()
        {
            Current = maxHealth;
            // Cached once — don't add or remove modifiers at runtime without calling RefreshModifiers.
            _modifiers = GetComponents<IDamageModifier>();
        }

        /// Call this if you add or remove an IDamageModifier at runtime.
        public void RefreshModifiers() => _modifiers = GetComponents<IDamageModifier>();

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;

            float damage = info.Amount;

            for (int i = 0; i < _modifiers.Length; i++)
            {
                damage = _modifiers[i].ModifyDamage(damage, info);
            }

            damage = Mathf.Max(0f, damage);
            if (damage <= 0f) return;

            Current = Mathf.Max(0f, Current - damage);

            OnDamaged?.Invoke(info, damage);
            OnChanged?.Invoke();
            onDamagedEvent?.Invoke();

            if (Current <= 0f) Die(info);
        }

        /// Restores a flat amount. Returns how much was actually restored.
        public float Heal(float amount)
        {
            if (IsDead || amount <= 0f) return 0f;

            float before = Current;
            Current = Mathf.Min(maxHealth, Current + amount);
            float restored = Current - before;

            if (restored > 0f)
            {
                OnHealed?.Invoke(restored);
                OnChanged?.Invoke();
            }

            return restored;
        }

        /// Restores a fraction of MAX health (0.1 = 10% of max, not 10% of what's missing).
        public float HealPercentOfMax(float fraction01) => Heal(maxHealth * Mathf.Max(0f, fraction01));

        public void SetMaxHealth(float newMax, bool refill)
        {
            maxHealth = Mathf.Max(1f, newMax);
            Current = refill ? maxHealth : Mathf.Min(Current, maxHealth);
            OnChanged?.Invoke();
        }

        /// Back to full and alive. Useful for pooling enemies or respawning the player.
        public void ResetHealth()
        {
            IsDead = false;
            Current = maxHealth;
            OnChanged?.Invoke();
        }

        private void Die(DamageInfo killingBlow)
        {
            IsDead = true;

            // Listeners (AmmoDropper, WaveManager, enemy cleanup) run before anything is destroyed.
            OnDied?.Invoke(killingBlow);
            onDiedEvent?.Invoke();

            if (destroyOnDeath) Destroy(gameObject, destroyDelay);
        }
    }
}
