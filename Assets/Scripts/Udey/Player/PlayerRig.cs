using UnityEngine;
using Udey.Core;

/// One place for anything that needs to find the player — enemies, the wave spawner, the
/// ammo drop system, the HUD. Saves scattering FindObjectOfType calls everywhere.

/// Put this on the Player root. Execution order is forced early so Instance is set before
/// anything else's Start runs.

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Health))]
public class PlayerRig : MonoBehaviour
{
    public static PlayerRig Instance { get; private set; }

    [Tooltip("The camera transform. Enemies aim at the body, weapons shoot from here.")]
    [SerializeField] private Transform aimOrigin;

    [Tooltip("Roughly chest height on the player, used as the aim point for enemy attacks.")]
    [SerializeField] private Transform centreOfMass;

    public Health Health { get; private set; }
    public PlayerHealth PlayerHealth { get; private set; }
    public AmmoInventory Ammo { get; private set; }
    public WeaponController Weapons { get; private set; }
    public PlayerMovement Movement { get; private set; }

    public Transform AimOrigin => aimOrigin != null ? aimOrigin : transform;
    public Transform CentreOfMass => centreOfMass != null ? centreOfMass : transform;

    public bool IsAlive => Health != null && !Health.IsDead;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerRig] A second PlayerRig appeared. Keeping the first one.", this);
            return;
        }

        Instance = this;

        Health = GetComponent<Health>();
        PlayerHealth = GetComponent<PlayerHealth>();
        Ammo = GetComponent<AmmoInventory>();
        Weapons = GetComponent<WeaponController>();
        Movement = GetComponent<PlayerMovement>();

        if (aimOrigin == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null) aimOrigin = childCamera.transform;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
