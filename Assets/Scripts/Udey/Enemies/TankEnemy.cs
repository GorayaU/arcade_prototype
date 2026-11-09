using UnityEngine;
using UnityEngine.Events;
using Udey.Core;

/// Tank — 80 HP, shield halves frontal damage, plods normally but charges to close distance,
/// then swipes with the shield up close.

/// Add a FrontalShield component alongside this one. The charge is a gap-closer, so it deals
/// no contact damage by default (set Charge Impact Damage above 0 if you want it to hurt).

/// Set Max Health to 80 on the Health component.

[RequireComponent(typeof(FrontalShield))]
public class TankEnemy : EnemyBase
{
    private enum State { Chasing, ChargeWindUp, Charging, Recovering, SwipeWindUp, Swiping }

    [Header("Tank — charge")]
    [Tooltip("Only charges when the player is at least this far away.")]
    [SerializeField] private float chargeTriggerDistance = 12f;

    [Tooltip("Visible tell before the charge launches.")]
    [SerializeField] private float chargeWindUpTime = 0.6f;

    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeMaxDuration = 1.6f;

    [Tooltip("Seconds before it can charge again.")]
    [SerializeField] private float chargeCooldown = 6f;

    [Tooltip("How hard it can steer mid-charge. 0 = fully committed, 1 = homing. Keep it low.")]
    [Range(0f, 1f)][SerializeField] private float chargeSteering = 0.18f;

    [Tooltip("Contact damage during the charge. 0 by default — the charge is for closing " +
             "distance, the swipe is the damage.")]
    [SerializeField] private float chargeImpactDamage = 0f;

    [SerializeField] private float chargeRecoverTime = 1.1f;

    [Header("Tank — shield swipe")]
    [SerializeField] private float swipeRange = 3f;
    [SerializeField] private float swipeWindUpTime = 0.45f;
    [SerializeField] private float swipeDuration = 0.2f;
    [SerializeField] private float swipeCooldown = 2f;
    [SerializeField] private float swipeDamage = 20f;

    [Tooltip("Wide — it's a shoulder-barge with a shield, not a precise jab.")]
    [Range(10f, 360f)][SerializeField] private float swipeArc = 120f;

    [Header("Tank — shield")]
    [Tooltip("Drop the shield while recovering from a charge, giving the player a punish " +
             "window. Off by default to match the brief exactly.")]
    [SerializeField] private bool lowerShieldDuringRecovery = false;

    [Header("Hooks (animation / audio / VFX)")]
    public UnityEvent onChargeWindUp;
    public UnityEvent onChargeStart;
    public UnityEvent onSwipeWindUp;
    public UnityEvent onSwipe;
    public UnityEvent onHitLanded;

    private State state = State.Chasing;
    private FrontalShield shield;
    private Vector3 chargeDirection;
    private bool hasHitThisAttack;
    private float nextChargeTime;
    private float nextSwipeTime;

    public bool IsTelegraphing => state == State.ChargeWindUp || state == State.SwipeWindUp;
    public bool IsCharging => state == State.Charging;

    private void Reset()
    {
        moveSpeed = 1.8f;      // slow plod
        detectionRange = 70f;
        turnSpeedDegrees = 240f;
    }

    protected override void Awake()
    {
        base.Awake();
        shield = GetComponent<FrontalShield>();
    }

    protected override void Tick(float deltaTime)
    {
        switch (state)
        {
            case State.Chasing: TickChasing(deltaTime); break;
            case State.ChargeWindUp: TickChargeWindUp(deltaTime); break;
            case State.Charging: TickCharging(deltaTime); break;
            case State.Recovering: TickRecovering(deltaTime); break;
            case State.SwipeWindUp: TickSwipeWindUp(deltaTime); break;
            case State.Swiping: TickSwiping(deltaTime); break;
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

        bool canSee = HasLineOfSightToTarget();

        if (DistanceToTarget <= swipeRange && Time.time >= nextSwipeTime && canSee)
        {
            EnterSwipeWindUp();
        }
        else if (DistanceToTarget >= chargeTriggerDistance && Time.time >= nextChargeTime && canSee)
        {
            EnterChargeWindUp();
        }
    }

    private void TickChargeWindUp(float deltaTime)
    {
        FaceTarget(deltaTime, 0.9f);

        if (StateTimer >= chargeWindUpTime) EnterCharging();
    }

    private void TickCharging(float deltaTime)
    {
        // Slight steering so it isn't trivially sidestepped, but still committed.
        if (chargeSteering > 0f && HasTarget)
        {
            Vector3 toTarget = Vector3.ProjectOnPlane(Target.position - transform.position, Vector3.up);
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                chargeDirection = Vector3.Slerp(
                    chargeDirection, toTarget.normalized, chargeSteering * deltaTime * 5f).normalized;
            }
        }

        FaceDirection(chargeDirection, deltaTime, 2f);
        DriveAlong(chargeDirection, chargeSpeed, deltaTime);

        if (chargeImpactDamage > 0f && !hasHitThisAttack &&
            TryHitTarget(chargeImpactDamage, swipeRange, 140f))
        {
            hasHitThisAttack = true;
            onHitLanded?.Invoke();
            EnterRecover();
            return;
        }

        // Stop once we're in swipe range, or when the charge times out.
        if (StateTimer >= chargeMaxDuration || DistanceToTarget <= swipeRange * 0.9f)
        {
            EnterRecover();
        }
    }

    private void TickRecovering(float deltaTime)
    {
        FaceTarget(deltaTime, 0.4f);

        if (StateTimer >= chargeRecoverTime)
        {
            if (lowerShieldDuringRecovery && shield != null) shield.IsRaised = true;
            EnterChase();
        }
    }

    private void TickSwipeWindUp(float deltaTime)
    {
        FaceTarget(deltaTime, 0.75f);

        if (StateTimer >= swipeWindUpTime) EnterSwiping();
    }

    private void TickSwiping(float deltaTime)
    {
        if (!hasHitThisAttack && TryHitTarget(swipeDamage, swipeRange, swipeArc))
        {
            hasHitThisAttack = true;
            onHitLanded?.Invoke();
        }

        if (StateTimer >= swipeDuration)
        {
            nextSwipeTime = Time.time + swipeCooldown;
            EnterChase();
        }
    }

    // ---------------------------------------------------------------- transitions

    private void EnterChase()
    {
        state = State.Chasing;
        ResetStateTimer();
    }

    private void EnterChargeWindUp()
    {
        state = State.ChargeWindUp;
        ResetStateTimer();
        StopMoving();
        onChargeWindUp?.Invoke();
    }

    private void EnterCharging()
    {
        state = State.Charging;
        ResetStateTimer();
        hasHitThisAttack = false;

        chargeDirection = HasTarget
            ? Vector3.ProjectOnPlane(Target.position - transform.position, Vector3.up)
            : transform.forward;

        if (chargeDirection.sqrMagnitude < 0.0001f) chargeDirection = transform.forward;
        chargeDirection.Normalize();

        StopMoving(); // hand control to DriveAlong for the duration of the charge
        onChargeStart?.Invoke();
    }

    private void EnterRecover()
    {
        state = State.Recovering;
        ResetStateTimer();
        StopMoving();

        nextChargeTime = Time.time + chargeCooldown;

        if (lowerShieldDuringRecovery && shield != null) shield.IsRaised = false;
    }

    private void EnterSwipeWindUp()
    {
        state = State.SwipeWindUp;
        ResetStateTimer();
        hasHitThisAttack = false;
        StopMoving();
        onSwipeWindUp?.Invoke();
    }

    private void EnterSwiping()
    {
        state = State.Swiping;
        ResetStateTimer();
        onSwipe?.Invoke();
    }

    protected override void HandleDeath(DamageInfo killingBlow)
    {
        if (shield != null) shield.IsRaised = false;
        base.HandleDeath(killingBlow);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, chargeTriggerDistance);
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, swipeRange);
    }
#endif
}
