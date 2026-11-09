using UnityEngine;
using UnityEngine.Events;

 
/// Skalith — 20 HP, slow, closes in and commits to a fast lunge.

/// The lunge is deliberately readable: it stops, winds up for a beat (that's the tell),
/// then commits to a direction it picked at the START of the lunge, so a player who moves
/// during the wind-up gets missed. Afterwards it's locked out of attacking for a second or
/// two, which is the window you're meant to punish.

/// Set Max Health to 20 on the Health component.

public class SkalithEnemy : EnemyBase
{
    private enum State { Chasing, WindUp, Lunging, Recovering }

    [Header("Skalith — lunge")]
    [Tooltip("Starts the wind-up once the player is this close.")]
    [SerializeField] private float lungeTriggerRange = 4.5f;

    [Tooltip("The telegraph. Long enough to read, short enough to feel dangerous.")]
    [SerializeField] private float windUpTime = 0.45f;

    [SerializeField] private float lungeSpeed = 13f;
    [SerializeField] private float lungeDuration = 0.3f;

    [Header("Skalith — recovery")]
    [Tooltip("Can't attack for this long afterwards. The brief says a second or two.")]
    [SerializeField] private float recoverTime = 1.6f;

    [Tooltip("Randomises recovery by +/- this, so a pack of them doesn't attack in lockstep.")]
    [SerializeField] private float recoverVariance = 0.35f;

    [Header("Skalith — damage")]
    [SerializeField] private float lungeDamage = 18f;
    [SerializeField] private float hitReach = 2.3f;
    [Range(10f, 360f)][SerializeField] private float hitArc = 110f;

    [Header("Hooks (animation / audio / VFX)")]
    public UnityEvent onWindUp;
    public UnityEvent onLunge;
    public UnityEvent onHitLanded;
    public UnityEvent onRecover;

    private State state = State.Chasing;
    private Vector3 lungeDirection;
    private bool hasHitThisLunge;
    private float currentRecoverTime;

    /// True while the wind-up is playing — hook your telegraph VFX to this.
    public bool IsTelegraphing => state == State.WindUp;

    private void Reset()
    {
        moveSpeed = 2f;       // slow
        detectionRange = 50f;
    }

    protected override void Tick(float deltaTime)
    {
        switch (state)
        {
            case State.Chasing: TickChasing(deltaTime); break;
            case State.WindUp: TickWindUp(deltaTime); break;
            case State.Lunging: TickLunging(deltaTime); break;
            case State.Recovering: TickRecovering(deltaTime); break;
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

        if (DistanceToTarget <= lungeTriggerRange && HasLineOfSightToTarget())
        {
            EnterWindUp();
        }
    }

    private void TickWindUp(float deltaTime)
    {
        // Still tracks, but slowly — the player can out-strafe a committed lunge.
        FaceTarget(deltaTime, 0.55f);

        if (StateTimer >= windUpTime) EnterLunge();
    }

    private void TickLunging(float deltaTime)
    {
        DriveAlong(lungeDirection, lungeSpeed, deltaTime);

        if (!hasHitThisLunge && TryHitTarget(lungeDamage, hitReach, hitArc))
        {
            hasHitThisLunge = true;
            onHitLanded?.Invoke();
        }

        if (StateTimer >= lungeDuration) EnterRecover();
    }

    private void TickRecovering(float deltaTime)
    {
        FaceTarget(deltaTime, 0.3f);

        if (StateTimer >= currentRecoverTime) EnterChase();
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

    private void EnterLunge()
    {
        state = State.Lunging;
        ResetStateTimer();
        hasHitThisLunge = false;

        // Commit to the direction now. This is what makes the lunge dodgeable.
        lungeDirection = HasTarget
            ? Vector3.ProjectOnPlane(Target.position - transform.position, Vector3.up)
            : transform.forward;

        if (lungeDirection.sqrMagnitude < 0.0001f) lungeDirection = transform.forward;
        lungeDirection.Normalize();

        onLunge?.Invoke();
    }

    private void EnterRecover()
    {
        state = State.Recovering;
        ResetStateTimer();
        StopMoving();
        currentRecoverTime = Mathf.Max(0.1f, recoverTime + Random.Range(-recoverVariance, recoverVariance));
        onRecover?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, lungeTriggerRange);
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, hitReach);
    }
#endif
}
