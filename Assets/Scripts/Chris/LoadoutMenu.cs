using UnityEngine;

public class LoadoutMenu : MonoBehaviour
{
    public static bool weaponSelected;
    [SerializeField] GameObject loadoutMenu;
    PauseMenu pauseMenu;
    PlayerShooting player;

    void Start()
    {
        weaponSelected = false;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerShooting>();
        loadoutMenu.SetActive(true);
        pauseMenu = GetComponent<PauseMenu>();
        pauseMenu.Pause();
    }

    public void ChooseRifle()
    {
        player.AssignSecondWeapon("Rifle");
        weaponSelected = true;
        loadoutMenu.SetActive(false);
        pauseMenu.Resume();
    }

    public void ChooseShotgun()
    {
        player.AssignSecondWeapon("Shotgun");
        weaponSelected = true;
        loadoutMenu.SetActive(false);
        pauseMenu.Resume();
    }
}
