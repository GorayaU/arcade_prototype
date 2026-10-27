using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Marks one screen of a menu — Main, Play, Endless, Settings, Pause, and so on.
/// Put it on the panel's root GameObject, the one that gets switched on and off.
///
/// It doesn't do the switching itself; MenuNavigator does. This carries the
/// per-panel details: which button to highlight when it opens, which other objects
/// to hide while it's up, and hooks for any animation or audio you want to fire.
/// </summary>
public class MenuPanel : MonoBehaviour
{
    [Tooltip("Button highlighted when this panel opens, so keyboard and gamepad have " +
             "somewhere to start. Leave empty for a mouse-only menu.")]
    [SerializeField] private Selectable firstSelected;

    [Tooltip("Objects switched OFF while this panel is open, and back ON when it closes. " +
             "Use it to hide the buttons behind this screen — e.g. the Play panel hides " +
             "Play, Settings and Exit. They don't have to be children of this panel.")]
    [SerializeField] private List<GameObject> hideWhileOpen = new List<GameObject>();

    [Tooltip("Pressing Escape / Back on this panel does nothing instead of going back. " +
             "Tick this on the root panel of a menu.")]
    [SerializeField] private bool isRoot = false;

    [Header("Hooks")]
    public UnityEvent onShown;
    public UnityEvent onHidden;

    public Selectable FirstSelected => firstSelected;
    public bool IsRoot => isRoot;

    /// <summary>
    /// How many open panels currently want each object hidden.
    /// Counting matters: going Play > Endless closes Play and opens Endless in the same
    /// frame, and without a count the Exit button would flicker back on in between.
    /// </summary>
    private static readonly Dictionary<GameObject, int> HideCounts = new Dictionary<GameObject, int>();

    /// <summary>Called by MenuNavigator — don't call this yourself to open a panel.</summary>
    public void NotifyShown()
    {
        for (int i = 0; i < hideWhileOpen.Count; i++) PushHide(hideWhileOpen[i]);
        onShown?.Invoke();
    }

    /// <summary>Called by MenuNavigator — don't call this yourself to close a panel.</summary>
    public void NotifyHidden()
    {
        for (int i = 0; i < hideWhileOpen.Count; i++) PopHide(hideWhileOpen[i]);
        onHidden?.Invoke();
    }

    private static void PushHide(GameObject target)
    {
        if (target == null) return;

        HideCounts.TryGetValue(target, out int count);
        HideCounts[target] = count + 1;

        if (count == 0) target.SetActive(false);
    }

    private static void PopHide(GameObject target)
    {
        if (target == null) return;
        if (!HideCounts.TryGetValue(target, out int count)) return;

        count--;

        if (count <= 0)
        {
            HideCounts.Remove(target);
            target.SetActive(true);
        }
        else
        {
            HideCounts[target] = count;
        }
    }

    /// <summary>
    /// Wipes the counts. MenuNavigator calls this on Start so a scene reload doesn't
    /// inherit stale entries pointing at destroyed objects.
    /// </summary>
    public static void ResetHideCounts() => HideCounts.Clear();
}
