using UnityEngine;

public class WeaponLocation : MonoBehaviour
{
    GameObject equippedWeapon;

    // Hides the weapon being switched out and makes the current one visible
    public void WeaponSwitched(int weaponIndex)
    {
        if (equippedWeapon != null)
        {
            equippedWeapon.SetActive(false);
        }
        equippedWeapon = transform.GetChild(weaponIndex).gameObject;
        equippedWeapon.SetActive(true);
    }
}
