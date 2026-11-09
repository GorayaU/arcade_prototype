using UnityEngine;
using UnityEngine.AI;
using Udey.Core;

/// Shared plumbing for every enemy: the NavMeshAgent, finding the player, turning to face
/// them, landing a melee hit in an arc, and shutting down cleanly on death.

/// Subclasses implement Tick() and run their own little state machine inside it.

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float acceleration = 20f;

    [Tooltip("How fast the enemy can rotate to face the player, in degrees per second.")]
    [SerializeField] protected float turnSpeedDegrees = 540f;

    [Header("Targeting")]
    [Tooltip("Beyond this the enemy idles instead of chasing.")]
    [SerializeField] protected float detectionRange = 60f;

    [Tooltip("Only attack when the player is actually visible. Costs one raycast per attack check.")]
    [SerializeField] protected bool requireLineOfSight = false;

    [Tooltip("Layers that block sight. Usually just your level geometry layer.")]
    [SerializeField] protected LayerMask sightBlockers = 1;

    [Tooltip("Eye position for line-of-sight checks. Defaults to 1.5m above the pivot.")]
    [SerializeField] protected Transform eye;

    [Header("Repathing")]
    [Tooltip("Seconds between SetDestination calls. Lower is more responsive, higher is cheaper.")]
    [SerializeField] protected float repathInterval = 0.15f;

    // --- Shared state for subclasses ---

    protected NavMeshAgent Agent { get; private set; }
    protected Health EnemyHealth { get; private set; }
    protected Transform Target { get; private set; }
    protected IDamageable TargetDamageable { get; private set; }

    /// Horizontal distance to the player. Infinity when there's no target.
    protected float DistanceToTarget { get; private set; } = Mathf.Infinity;

    /// Seconds since the last ResetStateTimer(). Subclasses time their states with this.
    protected float StateTimer { get; private set; }

    protected bool HasTarget => Target != null && TargetDamageable != null && !TargetDamageable.IsDead;

    private float nextRepathTime;
    private bool isDead;

    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        EnemyHealth = GetComponent<Health>();

        Agent.speed = moveSpeed;
        Agent.acceleration = acceleration;
        Agent.angularSpeed = turnSpeedDegrees;
        // We rotate manually so wind-ups and lunges point exactly where we want.
        Agent.updateRotation = false;

        EnemyHealth.OnDied += HandleDeath;
    }

    protected virtual void OnDestroy()
    {
        if (EnemyHealth != null) EnemyHealth.OnDied -= HandleDeath;
    }

    protected virtual void Start()
    {
        AcquireTarget();
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (!HasTarget) AcquireTarget();

        DistanceToTarget = HasTarget ? FlatDistanceTo(Target.position) : Mathf.Infinity;
        StateTimer += Time.deltaTime;

        Tick(Time.deltaTime);
    }

    /// One frame of this enemy's behaviour. Runs only while alive.
    protected abstract void Tick(float deltaTime);

    // ---------------------------------------------------------------- targeting

    private void AcquireTarget()
    {
        PlayerRig player = PlayerRig.Instance;
        if (player == null || !player.IsAlive)
        {
            Target = null;
            TargetDamageable = null;
            return;
        }

        Target = player.CentreOfMass;
        TargetDamageable = player.Health;
    }

    protected float FlatDistanceTo(Vector3 worldPosition)
    {
        Vector3 delta = worldPosition - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    protected bool TargetInDetectionRange => HasTarget && DistanceToTarget <= detectionRange;

    protected bool HasLineOfSightToTarget()
    {
        if (!HasTarget) return false;
        if (!requireLineOfSight) return true;

        Vector3 from = eye != null ? eye.position : transform.position + Vector3.up * 1.5f;
        Vector3 to = Target.position;

        return !Physics.Linecast(from, to, sightBlockers, QueryTriggerInteraction.Ignore);
    }

    // ---------------------------------------------------------------- movement helpers

    /// Path toward the player at the given speed. Repathing is throttled internally.
    protected void ChaseTarget(float speed)
    {
        if (!HasTarget || !Agent.isOnNavMesh) return;

        Agent.isStopped = false;
        Agent.speed = speed;

        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;
            Agent.SetDestination(Target.position);
        }
    }

    /// Plant the enemy where it stands — used during wind-ups and recovery.
    protected void StopMoving()
    {
        if (!Agent.isOnNavMesh) return;

        // Already parked — don't call ResetPath every frame while idling.
        if (Agent.isStopped && !Agent.hasPath) return;

        Agent.isStopped = true;
        Agent.velocity = Vector3.zero;
        Agent.ResetPath();
        nextRepathTime = 0f;
    }

    /// 
    /// Drive the enemy manually along a direction while staying stuck to the NavMesh.
    /// Used for lunges and charges, where pathing would be too slow to react.
    /// 
    protected void DriveAlong(Vector3 worldDirection, float speed, float deltaTime)
    {
        if (!Agent.isOnNavMesh) return;

        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude < 0.0001f) return;

        Agent.Move(worldDirection.normalized * speed * deltaTime);
    }

    /// Rotate toward the player at turnSpeedDegrees. Call every frame you want tracking.
    protected void FaceTarget(float deltaTime, float speedMultiplier = 1f)
    {
        if (!HasTarget) return;
        FaceDirection(Target.position - transform.position, deltaTime, speedMultiplier);
    }

    protected void FaceDirection(Vector3 direction, float deltaTime, float speedMultiplier = 1f)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion wanted = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, wanted, turnSpeedDegrees * speedMultiplier * deltaTime);
    }

    // ---------------------------------------------------------------- attacking
    
    /// Applies damage to the player if they're inside the given reach and arc.
    /// Returns true if it connected, so the caller can branch on hit/whiff.
    protected bool TryHitTarget(float damage, float reach, float arcDegrees)
    {
        if (!HasTarget) return false;
        if (DistanceToTarget > reach) return false;

        Vector3 toTarget = Target.position - transform.position;
        toTarget.y = 0f;

        if (arcDegrees < 360f && Vector3.Angle(transform.forward, toTarget) > arcDegrees * 0.5f) return false;
        if (!HasLineOfSightToTarget()) return false;

        Vector3 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : transform.forward;
        TargetDamageable.TakeDamage(new DamageInfo(
            damage, DamageType.Melee, Target.position, direction, gameObject));

        return true;
    }

    // ---------------------------------------------------------------- state / death

    protected void ResetStateTimer() => StateTimer = 0f;

    protected virtual void HandleDeath(DamageInfo killingBlow)
    {
        isDead = true;

        if (Agent != null && Agent.isOnNavMesh) Agent.isStopped = true;
        if (Agent != null) Agent.enabled = false;

        // Stop blocking shots and stop triggering the player's melee.
        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        enabled = false;
    }
}
