using UnityEngine;
using UnityEngine.Events;

/// Soldier — 35 HP, moves a little slower than the player, pokes with a long spear.

/// The threat here is reach, not speed: you can always walk away from one, but its spear
/// outranges anything you'd expect at that distance, so backpedalling in a straight line
/// still gets you hit. Short wind-up, short damage window, then a cooldown.

/// Set Max Health to 35 on the Health component.
/// Player walk speed is 5, so 4.2 keeps it just behind you.

public class SoldierEnemy : EnemyBase
{
    private enum State { Chasing, WindUp, Thrusting, Cooldown }

    [Header("Soldier — spear")]
    [Tooltip("Deliberately longer than it looks. This is the whole character.")]
    [SerializeField] private float spearReach = 3.6f;

    [Tooltip("Starts the wind-up at this range, slightly inside the spear's actual reach.")]
    [SerializeField] private float attackTriggerRange = 3.2f;

    [SerializeField] private float windUpTime = 0.35f;

    [Tooltip("How long the spear is actually out and dangerous.")]
    [SerializeField] private float thrustDuration = 0.18f;

    [SerializeField] private float cooldownTime = 1.25f;

    [Tooltip("Small forward step on the thrust, so it has weight and can clip a retreating player.")]
    [SerializeField] private float thrustLungeSpeed = 4f;

    [Header("Soldier — damage")]
    [SerializeField] private float spearDamage = 15f;

    [Tooltip("Narrow — it's a straight thrust, not a swing. Strafing beats it.")]
    [Range(10f, 360f)][SerializeField] private float thrustArc = 55f;

    [Header("Hooks (animation / audio / VFX)")]
    public UnityEvent onWindUp;
    public UnityEvent onThrust;
    public UnityEvent onHitLanded;

    private State state = State.Chasing;
    private Vector3 thrustDirection;
    private bool hasHitThisThrust;

    public bool IsTelegraphing => state == State.WindUp;

    private void Reset()
    {
        moveSpeed = 4.2f;     // player walks at 5
        detectionRange = 60f;
    }

    protected override void Tick(float deltaTime)
    {
        switch (state)
        {
            case State.Chasing: TickChasing(deltaTime); break;
            case State.WindUp: TickWindUp(deltaTime); break;
            case State.Thrusting: TickThrusting(deltaTime); break;
            case State.Cooldown: TickCooldown(deltaTime); break;
        }
    }

    private void TickChasing(float deltaTime)
    {
        if (!TargetInDetectionRange)
        {
            StopMoving();
            return;
        }

        FaceTarget(deltaTime);
        ChaseTarget(moveSpeed);

        if (DistanceToTarget <= attackTriggerRange && HasLineOfSightToTarget())
        {
            EnterWindUp();
        }
    }

    private void TickWindUp(float deltaTime)
    {
        // Tracks well during the wind-up — you beat this by moving sideways, not by backing off.
        FaceTarget(deltaTime, 0.8f);

        if (StateTimer >= windUpTime) EnterThrust();
    }

    private void TickThrusting(float deltaTime)
    {
        DriveAlong(thrustDirection, thrustLungeSpeed, deltaTime);

        if (!hasHitThisThrust && TryHitTarget(spearDamage, spearReach, thrustArc))
        {
            hasHitThisThrust = true;
            onHitLanded?.Invoke();
        }

        if (StateTimer >= thrustDuration) EnterCooldown();
    }

    private void TickCooldown(float deltaTime)
    {
        // Backs off slightly instead of standing still, so it resets to spear range.
        FaceTarget(deltaTime, 0.6f);

        if (StateTimer >= cooldownTime) EnterChase();
    }

    // ---------------------------------------------------------------- transitions

    private void EnterChase()
    {
        state = State.Chasing;
        ResetStateTimer();
    }

    private void EnterWindUp()
    {
        state = State.WindUp;
        ResetStateTimer();
        StopMoving();
        onWindUp?.Invoke();
    }

    private void EnterThrust()
    {
        state = State.Thrusting;
        ResetStateTimer();
        hasHitThisThrust = false;

        thrustDirection = HasTarget
            ? Vector3.ProjectOnPlane(Target.position - transform.position, Vector3.up)
            : transform.forward;

        if (thrustDirection.sqrMagnitude < 0.0001f) thrustDirection = transform.forward;
        thrustDirection.Normalize();

        onThrust?.Invoke();
    }

    private void EnterCooldown()
    {
        state = State.Cooldown;
        ResetStateTimer();
        StopMoving();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, attackTriggerRange);
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, spearReach);
    }
#endif
}
