using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Pauses the game and shows the pause UI. Lives in the gameplay scene, on an object
/// that stays active — the Canvas is the obvious choice. Never put it on the pause panel
/// itself: an inactive GameObject doesn't run Update, so it could never un-pause.
///
/// Two things have to happen together, and missing either one is the usual bug:
///   - Time.timeScale = 0 freezes movement, enemies and the wave coroutines.
///   - The player's scripts get switched off, because Update still runs at timeScale 0.
///     Without this you shoot through the menu, and MouseLook keeps grabbing the cursor.
///
/// The player scripts are found through PlayerRig, so there is nothing to wire up.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("The pause panel. Leave it inactive in the scene.")]
    [SerializeField] private GameObject pauseRoot;

    [Tooltip("Optional. Assign if the pause menu has sub-panels (settings, confirm quit). " +
             "Turn OFF its Back On Escape — this script forwards Escape to it.")]
    [SerializeField] private MenuNavigator navigator;

    [Header("Gameplay")]
    [Tooltip("Find PlayerMovement, MouseLook and WeaponController through PlayerRig and " +
             "switch them off while paused. Leave this on.")]
    [SerializeField] private bool autoFindPlayerScripts = true;

    [Tooltip("Anything EXTRA to switch off while paused. You don't need the three player " +
             "scripts here. Never add PlayerInput — the player scripts cache their actions " +
             "from it, and disabling it pulls the rug out from under them.")]
    [SerializeField] private List<MonoBehaviour> alsoDisableWhilePaused = new List<MonoBehaviour>();

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("Input")]
    [SerializeField] private bool pauseWithEscape = true;

    /// <summary>Readable from anywhere — useful for gating other systems.</summary>
    public static bool IsPaused { get; private set; }

    /// <summary>Whatever we actually switched off, so Resume turns exactly that back on.</summary>
    private readonly List<Behaviour> suspended = new List<Behaviour>();

    private void Awake()
    {
        // A fresh scene is never paused, even if the last one quit while paused.
        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseRoot != null) pauseRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        // Never leave the next scene with time stopped.
        if (IsPaused) Time.timeScale = 1f;
        IsPaused = false;
    }

    private void Update()
    {
        if (!pauseWithEscape || Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (!IsPaused)
        {
            Pause();
            return;
        }

        // While paused, Escape backs out of a sub-panel first, and only resumes
        // once we're at the top of the pause menu.
        if (navigator != null && navigator.CanGoBack) navigator.Back();
        else Resume();
    }

    // ---------------------------------------------------------------- pause / resume

    public void Pause()
    {
        if (IsPaused) return;

        IsPaused = true;
        Time.timeScale = 0f;

        SuspendGameplayScripts();

        if (pauseRoot != null) pauseRoot.SetActive(true);
        if (navigator != null) navigator.OpenRoot();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = 1f;

        if (navigator != null) navigator.CloseAll();
        if (pauseRoot != null) pauseRoot.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Restore next frame, not now. Resume is usually triggered by Escape, and MouseLook
        // would see that same key press and immediately free the cursor again.
        StartCoroutine(RestoreGameplayNextFrame());
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    // ---------------------------------------------------------------- leaving

    public void ReturnToMainMenu()
    {
        // Unpause BEFORE loading, or the menu scene starts with time stopped and
        // every animation and coroutine in it sits still.
        IsPaused = false;
        Time.timeScale = 1f;
        suspended.Clear();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[PauseMenu] Quit");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------- internals

    /// <summary>
    /// Collected fresh on every pause rather than cached in Awake, so a player spawned
    /// after this script started is still picked up.
    /// </summary>
    private void SuspendGameplayScripts()
    {
        suspended.Clear();

        if (autoFindPlayerScripts)
        {
            PlayerRig rig = PlayerRig.Instance;

            if (rig == null)
            {
                Debug.LogWarning("[PauseMenu] No PlayerRig in the scene, so the player's scripts " +
                                 "can't be switched off. You'll still be able to shoot while paused.", this);
            }
            else
            {
                Suspend(rig.Movement);
                Suspend(rig.Weapons);
                Suspend(rig.GetComponent<MouseLook>());
            }
        }

        for (int i = 0; i < alsoDisableWhilePaused.Count; i++) Suspend(alsoDisableWhilePaused[i]);
    }

    private void Suspend(Behaviour target)
    {
        if (target == null || target == this) return;
        if (!target.enabled) return;        // already off — leave it off on resume
        if (suspended.Contains(target)) return;

        target.enabled = false;
        suspended.Add(target);
    }

    private IEnumerator RestoreGameplayNextFrame()
    {
        yield return null;

        for (int i = 0; i < suspended.Count; i++)
        {
            if (suspended[i] != null) suspended[i].enabled = true;
        }

        suspended.Clear();
    }
}
