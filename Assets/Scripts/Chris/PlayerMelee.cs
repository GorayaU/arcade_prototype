using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMelee : MonoBehaviour
{
    // Input actions
    [SerializeField] InputActionAsset InputActions;
    InputAction melee;

    // Attacking variables
    [Header("Attacking")]
    [SerializeField] GameObject weapon;
    [SerializeField] float meleeDamage = 12.0f;
    [SerializeField] float attackSpeed = 0.5f;
    PlayerShooting playerShooting;
    bool isAttacking;

    void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }

    void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }

    public float GetMeleeDamage()
    {
        return meleeDamage;
    }

    public bool GetIsMeleeAttacking()
    {
        return isAttacking;
    }

    void Awake()
    {
        melee = InputSystem.actions.FindAction("Melee");
        weapon.SetActive(false);
        playerShooting = GetComponent<PlayerShooting>();
        isAttacking = false;
    }

    void Update()
    {
        if (!PauseMenu.IsPaused)
        {
            if (melee.WasPerformedThisFrame() && !isAttacking)
            {
                MeleeAttack();
                StartCoroutine(AttackCooldown());
            }
        }
    }

    void MeleeAttack()
    {
        if (!playerShooting.GetReloading() && !playerShooting.GetSwitching())
        {
            weapon.SetActive(true);
        }
    }

    IEnumerator AttackCooldown()
    {
        isAttacking = true;
        yield return new WaitForSeconds(attackSpeed);
        weapon.SetActive(false);
        isAttacking = false;
    }
}
