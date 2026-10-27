using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Pauses the game and shows the pause UI. Lives in the gameplay scene.
///
/// Two things have to happen together, and missing either one is the usual bug:
///   - Time.timeScale = 0 freezes movement, enemies and the wave coroutines.
///   - The player's scripts get switched off, because Update still runs at timeScale 0.
///     Without this you shoot straight through the menu while paused.
///
/// Setup:
///   1. Put this on the Canvas, with the pause panel assigned to Pause Root (start it inactive).
///   2. Drag PlayerMovement, MouseLook and WeaponController into Disable While Paused.
///   3. Buttons: Resume → Resume, Main Menu → ReturnToMainMenu, Exit → QuitGame.
///
/// NOTE: MouseLook also listens for Escape to free the cursor. Having it in the
/// Disable While Paused list is what stops the two fighting over cursor state.
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
    [Tooltip("Switched off while paused. PlayerMovement, MouseLook, WeaponController.")]
    [SerializeField] private MonoBehaviour[] disableWhilePaused;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("Input")]
    [SerializeField] private bool pauseWithEscape = true;

    /// <summary>Readable from anywhere — useful for gating other systems.</summary>
    public static bool IsPaused { get; private set; }

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

        SetGameplayScriptsEnabled(false);

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

        // Re-enable next frame, not now. Resume is usually triggered by Escape, and
        // MouseLook would see that same key press and immediately free the cursor again.
        StartCoroutine(EnableGameplayNextFrame());
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

    private IEnumerator EnableGameplayNextFrame()
    {
        yield return null;
        SetGameplayScriptsEnabled(true);
    }

    private void SetGameplayScriptsEnabled(bool enabled)
    {
        for (int i = 0; i < disableWhilePaused.Length; i++)
        {
            if (disableWhilePaused[i] != null) disableWhilePaused[i].enabled = enabled;
        }
    }
}
