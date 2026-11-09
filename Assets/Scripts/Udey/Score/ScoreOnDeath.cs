using UnityEngine;
using UnityEngine.Events;
using Udey.Core;

/// <summary>
/// Awards score when this enemy dies. Put it on every enemy prefab next to AmmoDropper —
/// same pattern, same Health.OnDied hook.
///
/// "Harder enemies are worth more" lives here as a per-prefab number rather than a lookup
/// keyed on enemy type, so a new enemy needs no code change. Suggested values:
///   Skalith 100 · Soldier 175 · Tank 400
/// </summary>
[RequireComponent(typeof(Health))]
public class ScoreOnDeath : MonoBehaviour
{
    [Tooltip("Points before the difficulty multiplier. Scale it with how dangerous the " +
             "enemy is, not just its health.")]
    [SerializeField] private int basePoints = 100;

    [Tooltip("Extra multiplier when the killing blow was melee, matching the ammo-drop " +
             "bonus. 1 = no bonus.")]
    [SerializeField] private float meleeKillBonus = 1.25f;

    [Header("Hooks")]
    [Tooltip("Fires with the points awarded — hook a floating '+150' popup here.")]
    public UnityEvent onScoreAwarded;

    public int BasePoints { get { return basePoints; } }

    private Health health;
    private bool awarded;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDied;
    }

    private void HandleDied(DamageInfo killingBlow)
    {
        if (awarded) return;
        awarded = true;

        float points = basePoints;
        if (killingBlow.Type == DamageType.Melee) points *= Mathf.Max(1f, meleeKillBonus);

        // Award, not Instance.Award — this no-ops cleanly in a test scene with no manager.
        ScoreManager.Award(Mathf.RoundToInt(points));

        onScoreAwarded?.Invoke();
    }
}
