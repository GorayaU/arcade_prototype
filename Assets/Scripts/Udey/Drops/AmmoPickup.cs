using UnityEngine;


/// One magazine of ammo lying on the ground. Spawned by AmmoDropper, or placed by hand in
/// the level if you set Weapon in the inspector.

/// The collider must be a trigger, and the player needs a Rigidbody (or a CharacterController,
/// which also generates trigger events) for OnTriggerEnter to fire.

[RequireComponent(typeof(Collider))]
public class AmmoPickup : MonoBehaviour
{
    [Header("Contents")]
    [Tooltip("Left empty when spawned by an AmmoDropper — it fills this in. Set it by hand " +
             "only for pickups you place in the level yourself.")]
    [SerializeField] private WeaponDefinition weapon;

    [SerializeField] private int magazines = 1;

    [Header("Presentation")]
    [Tooltip("Tinted with the weapon's pickup colour. Optional.")]
    [SerializeField] private Renderer tintRenderer;

    [SerializeField] private float spinDegreesPerSecond = 90f;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 2.2f;

    [Header("Lifetime")]
    [Tooltip("Seconds before it despawns. 0 = never.")]
    [SerializeField] private float lifetime = 45f;

    private Vector3 basePosition;
    private float bobOffset;
    private bool collected;

    public WeaponDefinition Weapon => weapon;

    /// Called by AmmoDropper right after Instantiate.
    public void Configure(WeaponDefinition definition, int magazineCount)
    {
        weapon = definition;
        magazines = Mathf.Max(1, magazineCount);
        ApplyTint();
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        // Desync the bob so a pile of pickups doesn't move as one block.
        bobOffset = Random.value * Mathf.PI * 2f;
    }

    private void Start()
    {
        basePosition = transform.position;
        ApplyTint();

        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (spinDegreesPerSecond != 0f)
        {
            transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
        }

        if (bobHeight > 0f)
        {
            float y = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
            transform.position = basePosition + Vector3.up * y;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || weapon == null) return;

        PlayerRig player = other.GetComponentInParent<PlayerRig>();
        if (player == null || player.Ammo == null) return;

        // Already carrying the maximum — leave it on the ground for later.
        if (!player.Ammo.AddMagazines(weapon, magazines)) return;

        collected = true;
        Destroy(gameObject);
    }

    private void ApplyTint()
    {
        if (tintRenderer == null || weapon == null) return;

        // Instance material, so tinting one pickup doesn't recolour every pickup.
        tintRenderer.material.color = weapon.pickupColor;
    }
}
