using System;
using UnityEngine;
using UnityEngine.Events;
using Udey.Core;

/// Player-specific health policy, sitting next to the shared Health component:
/// the between-waves top-up, and what happens when you die.

/// Set Max Health to 100 on the Health component and leave Destroy On Death unticked.

[RequireComponent(typeof(Health))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Between waves")]
    [Tooltip("Percent of MAX health restored each time a wave is cleared. The brief says 10.")]
    [Range(0f, 100f)][SerializeField] private float healPercentPerWave = 10f;

    [Header("Death")]
    [Tooltip("Scripts switched off when the player dies — movement, look, weapons.")]
    [SerializeField] private MonoBehaviour[] disableOnDeath;

    [SerializeField] private bool freeCursorOnDeath = true;

    public UnityEvent onDeathEvent;

    public event Action OnDied;

    /// (amount healed) — fires after a wave is cleared.
    public event Action<float> OnWaveHeal;

    private Health health;

    public Health Health => health;
    public float HealPercentPerWave => healPercentPerWave;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDied += HandleDeath;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    /// Called by WaveManager once every enemy in a wave is down.
    public void OnWaveCleared()
    {
        if (health.IsDead) return;

        float healed = health.HealPercentOfMax(healPercentPerWave / 100f);
        if (healed > 0f) OnWaveHeal?.Invoke(healed);
    }

    private void HandleDeath(DamageInfo killingBlow)
    {
        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            if (disableOnDeath[i] != null) disableOnDeath[i].enabled = false;
        }

        if (freeCursorOnDeath)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        OnDied?.Invoke();
        onDeathEvent?.Invoke();
    }
}
