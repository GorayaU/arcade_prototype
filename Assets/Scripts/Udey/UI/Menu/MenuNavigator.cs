using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Switches between menu panels and remembers how you got there, so Back always
/// returns to the screen you came from without you wiring up a return path by hand.
///
/// Main Menu > Play > Endless > Hard is three Shows; one Back from the difficulty
/// screen lands on Play, another lands on Main. Nothing in the scene has to know
/// the shape of the tree.
///
/// Setup:
///   1. Put this on the Canvas (or an empty "Menu" object under it).
///   2. Drop every panel GameObject into Panels, and the one you open on into Root Panel.
///   3. On each Button, add an On Click entry targeting this component:
///        MenuNavigator.Show  → drag the panel to open into the object slot
///        MenuNavigator.Back  → for Back buttons
/// </summary>
public class MenuNavigator : MonoBehaviour
{
    [Tooltip("Every panel this navigator owns. All of them are switched off on Start, " +
             "then Root Panel is opened.")]
    [SerializeField] private List<GameObject> panels = new List<GameObject>();

    [Tooltip("The panel shown when the menu opens — Main Menu, or Pause.")]
    [SerializeField] private GameObject rootPanel;

    [Tooltip("Escape goes back one screen. Turn this OFF when a PauseMenu is driving " +
             "this navigator, or Escape gets handled twice.")]
    [SerializeField] private bool backOnEscape = true;

    [Tooltip("Open the root panel automatically on Start. Turn off for a pause menu, " +
             "which starts hidden.")]
    [SerializeField] private bool openOnStart = true;

    /// <summary>The panel currently on screen, or null if the menu is closed.</summary>
    public GameObject Current { get; private set; }

    /// <summary>True when there's a screen to go back to.</summary>
    public bool CanGoBack => history.Count > 0;

    private readonly List<GameObject> history = new List<GameObject>();

    private void Start()
    {
        // A scene reload would otherwise inherit hide counts for destroyed objects.
        MenuPanel.ResetHideCounts();

        HideAll();
        if (openOnStart) OpenRoot();
    }

    private void Update()
    {
        if (!backOnEscape || Current == null) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame) Back();
    }

    // ---------------------------------------------------------------- navigation

    /// <summary>Opens a panel and remembers the current one so Back can return to it.</summary>
    public void Show(GameObject panel)
    {
        if (panel == null || panel == Current) return;

        if (Current != null)
        {
            history.Add(Current);
            SetPanelActive(Current, false);
        }

        Current = panel;
        SetPanelActive(panel, true);
    }

    /// <summary>Returns to the previous panel. Does nothing at the root.</summary>
    public void Back()
    {
        if (history.Count == 0) return;

        MenuPanel current = Current != null ? Current.GetComponent<MenuPanel>() : null;
        if (current != null && current.IsRoot) return;

        int last = history.Count - 1;
        GameObject previous = history[last];
        history.RemoveAt(last);

        if (Current != null) SetPanelActive(Current, false);

        Current = previous;
        SetPanelActive(previous, true);
    }

    /// <summary>Jumps straight back to the root panel and forgets the trail.</summary>
    public void OpenRoot()
    {
        history.Clear();

        if (Current != null && Current != rootPanel) SetPanelActive(Current, false);

        Current = rootPanel;
        if (rootPanel != null) SetPanelActive(rootPanel, true);
    }

    /// <summary>Switches everything off — used when a pause menu closes.</summary>
    public void CloseAll()
    {
        if (Current != null) SetPanelActive(Current, false);
        Current = null;
        history.Clear();
        HideAll();
    }

    // ---------------------------------------------------------------- internals

    private void HideAll()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] != null && panels[i].activeSelf) SetPanelActive(panels[i], false);
        }
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel == null) return;

        panel.SetActive(active);

        MenuPanel menuPanel = panel.GetComponent<MenuPanel>();
        if (menuPanel == null) return;

        if (active)
        {
            menuPanel.NotifyShown();
            SelectFirst(menuPanel);
        }
        else
        {
            menuPanel.NotifyHidden();
        }
    }

    /// <summary>
    /// Highlights the panel's first button. Without this, keyboard and gamepad
    /// navigation has nothing selected and the menu feels broken on a controller.
    /// </summary>
    private void SelectFirst(MenuPanel panel)
    {
        if (panel.FirstSelected == null) return;
        if (EventSystem.current == null) return;

        // Clearing first makes the EventSystem re-fire the highlight even if the
        // same object was selected before.
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(panel.FirstSelected.gameObject);
    }
}
