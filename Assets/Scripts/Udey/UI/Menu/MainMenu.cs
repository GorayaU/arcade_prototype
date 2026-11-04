using UnityEngine;
using UnityEngine.SceneManagement;
using Udey.Core;

/// <summary>
/// The buttons that actually do something on the main menu: starting a run and quitting.
/// Moving between Play / Endless / Settings is MenuNavigator's job — this only handles
/// the leaves of the tree.
///
/// Button hookups:
///   Story    → MainMenu.PlayStory
///   Easy     → MainMenu.PlayEndless, int = 0
///   Medium   → MainMenu.PlayEndless, int = 1
///   Hard     → MainMenu.PlayEndless, int = 2
///   Exit     → MainMenu.QuitGame
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Scene loaded by Story. Must be in File > Build Profiles > Scene List.")]
    [SerializeField] private string storySceneName = "Story";

    [Tooltip("Scene loaded by Endless. Must be in the Scene List too.")]
    [SerializeField] private string endlessSceneName = "Udey Test Scene";

    [Header("Optional")]
    [Tooltip("Shown just before the scene load starts, so the menu doesn't appear frozen.")]
    [SerializeField] private GameObject loadingPanel;

    private void Start()
    {
        // Coming back from a paused game leaves time stopped and the cursor hidden.
        // The menu scene has no idea that happened, so reset both here.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ---------------------------------------------------------------- play

    public void PlayStory()
    {
        GameSession.StartStory();
        LoadScene(storySceneName);
    }

    /// <summary>0 = Easy, 1 = Medium, 2 = Hard. Set the int on the Button's On Click entry.</summary>
    public void PlayEndless(int difficultyIndex)
    {
        GameSession.StartEndless(difficultyIndex);
        LoadScene(endlessSceneName);
    }

    // ---------------------------------------------------------------- exit

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Quit");

#if UNITY_EDITOR
        // Application.Quit does nothing in the editor, so stop play mode instead.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------- internals

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[MainMenu] No scene name set for that button.", this);
            return;
        }

        if (loadingPanel != null) loadingPanel.SetActive(true);

        SceneManager.LoadScene(sceneName);
    }
}
