using UnityEngine;
using Udey.Core;

/// Health bar, driven by any Health component. Leave Health empty and it finds the player's.
/// Also works as a floating bar over an enemy — just assign that enemy's Health.

public class HealthBar : FillBar
{
    [Header("Source")]
    [Tooltip("Leave empty to use the player's Health.")]
    [SerializeField] private Health health;

    [Header("Critical")]
    [Tooltip("At or below this fraction the bar turns red and pulses.")]
    [Range(0f, 1f)][SerializeField] private float criticalThreshold = 0.25f;

    [Tooltip("Keep the bar visible for this long after taking a hit, even if Hide When Full is on.")]
    [SerializeField] private float showAfterDamageTime = 3f;

    private float visibleUntil;

    private void Reset()
    {
        // Health reads better as a horizontal bar; stamina as a vertical one.
        direction = FillDirection.Horizontal;
        hideWhenFull = false;
    }

    protected override bool ResolveSource()
    {
        if (health == null && PlayerRig.Instance != null) health = PlayerRig.Instance.Health;

        if (health == null)
        {
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null) health = playerHealth.Health;
        }

        if (health == null)
        {
            Debug.LogError("[HealthBar] No Health assigned and no player Health found.", this);
            return false;
        }

        health.OnDamaged += HandleDamaged;
        return true;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(DamageInfo info, float amount) => visibleUntil = Time.time + showAfterDamageTime;

    protected override float GetTargetValue01() => health.Normalized;

    protected override bool IsCritical => !health.IsDead && health.Normalized <= criticalThreshold;

    protected override bool ForceVisible => Time.time < visibleUntil;
}
