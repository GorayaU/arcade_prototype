using UnityEngine;
using Udey.Core;

/// Halves damage that arrives inside a cone in front of the enemy. Put this on the same
/// GameObject as the Health — Health finds every IDamageModifier on itself and runs them.

/// This is what makes the Tank a positioning puzzle: shoot it in the face and you waste
/// half your magazine, get behind it and it dies at the normal rate.

[RequireComponent(typeof(Health))]
public class FrontalShield : MonoBehaviour, IDamageModifier
{
    [Tooltip("Damage multiplier for hits inside the protected cone. 0.5 = halved.")]
    [Range(0f, 1f)][SerializeField] private float damageMultiplier = 0.5f;

    [Tooltip("Half-angle of the protected cone, measured from the enemy's forward direction. " +
             "70 means the shield covers a 140-degree arc across the front.")]
    [Range(0f, 180f)][SerializeField] private float protectedHalfAngle = 70f;

    [Tooltip("Does the shield also soak melee hits? Turn this off if you want melee to be " +
             "the answer to a shielded enemy.")]
    [SerializeField] private bool blocksMelee = true;

    /// Set false to open a damage window — see TankEnemy's lowerShieldDuringRecovery.
    public bool IsRaised { get; set; } = true;

    public float ModifyDamage(float incoming, DamageInfo info)
    {
        if (!IsRaised) return incoming;
        if (!blocksMelee && info.Type == DamageType.Melee) return incoming;

        // info.Direction is the way the attack was travelling, so flip it to get the
        // direction it came FROM.
        Vector3 fromAttacker = -info.Direction;
        fromAttacker.y = 0f;
        if (fromAttacker.sqrMagnitude < 0.0001f) return incoming;

        Vector3 forward = transform.forward;
        forward.y = 0f;

        bool fromTheFront = Vector3.Angle(forward, fromAttacker) <= protectedHalfAngle;
        return fromTheFront ? incoming * damageMultiplier : incoming;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up;
        Quaternion left = Quaternion.AngleAxis(-protectedHalfAngle, Vector3.up);
        Quaternion right = Quaternion.AngleAxis(protectedHalfAngle, Vector3.up);

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
        Gizmos.DrawRay(origin, left * transform.forward * 3f);
        Gizmos.DrawRay(origin, right * transform.forward * 3f);
        Gizmos.DrawRay(origin, transform.forward * 3f);
    }
#endif
}
