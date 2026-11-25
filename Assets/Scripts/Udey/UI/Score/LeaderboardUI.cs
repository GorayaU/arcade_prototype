using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Builds the high score table out of LeaderboardRow prefabs.
///
/// Works in two places with no changes: on a Leaderboard panel in the main menu, and on
/// the game-over screen, where it highlights the run you just finished.
///
/// Setup:
///   1. Panel > Scroll View (or just a plain object) with a Vertical Layout Group inside.
///   2. Assign that layout object to Content Parent and your row prefab to Row Prefab.
///   3. Refreshes itself whenever it's switched on, so MenuNavigator needs no extra wiring.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("Rows")]
    [Tooltip("Prefab with a LeaderboardRow on it.")]
    [SerializeField] private LeaderboardRow rowPrefab;

    [Tooltip("The object rows are parented to. Give it a Vertical Layout Group.")]
    [SerializeField] private Transform contentParent;

    [SerializeField] private int maxRows = 10;

    [Header("Filter")]
    [Tooltip("Show only one board: Story, Easy, Medium or Hard. Empty shows every run.")]
    [SerializeField] private string difficultyFilter = "";

    [Header("Extras")]
    [Tooltip("Shown instead of rows when nothing has been saved yet.")]
    [SerializeField] private GameObject emptyMessage;

    [Tooltip("Optional heading, e.g. 'HIGH SCORES — HARD'.")]
    [SerializeField] private TMP_Text headingText;

    [SerializeField] private string headingFormat = "HIGH SCORES";

    [Header("Game over")]
    [Tooltip("Highlight the run that just finished, if a ScoreManager is in this scene.")]
    [SerializeField] private bool highlightLatestRun = true;

    private readonly List<LeaderboardRow> spawned = new List<LeaderboardRow>();
    private ScoreEntry highlighted;
    private ScoreManager manager;

    private void Awake()
    {
        if (rowPrefab == null || contentParent == null)
        {
            Debug.LogError("[LeaderboardUI] Assign both Row Prefab and Content Parent.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (!highlightLatestRun) return;

        manager = ScoreManager.Instance;
        if (manager != null) manager.OnRunSubmitted += HandleRunSubmitted;
    }

    private void OnEnable()
    {
        // Rebuild every time the panel opens, so it can't show a stale table.
        Leaderboard.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Leaderboard.OnChanged -= Refresh;
    }

    private void OnDestroy()
    {
        if (manager != null) manager.OnRunSubmitted -= HandleRunSubmitted;
    }

    // ---------------------------------------------------------------- public

    /// <summary>Switches boards at runtime — hook this to Easy / Medium / Hard tabs.</summary>
    public void SetFilter(string difficulty)
    {
        difficultyFilter = difficulty;
        Refresh();
    }

    public void ShowAllDifficulties() => SetFilter("");

    /// <summary>Marks one entry so it stands out. Cleared by passing null.</summary>
    public void SetHighlight(ScoreEntry entry)
    {
        highlighted = entry;
        Refresh();
    }

    public void Refresh()
    {
        if (!enabled) return;

        ClearRows();

        List<ScoreEntry> entries = Leaderboard.GetTop(maxRows, difficultyFilter);

        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardRow row = Instantiate(rowPrefab, contentParent);
            row.Bind(i + 1, entries[i], entries[i] == highlighted);
            spawned.Add(row);
        }

        if (emptyMessage != null) emptyMessage.SetActive(entries.Count == 0);

        if (headingText != null)
        {
            headingText.text = string.IsNullOrEmpty(difficultyFilter)
                ? headingFormat
                : $"{headingFormat} — {difficultyFilter.ToUpper()}";
        }
    }

    // ---------------------------------------------------------------- internals

    private void HandleRunSubmitted(ScoreEntry entry, int rank)
    {
        highlighted = entry;
        Refresh();
    }

    private void ClearRows()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null) Destroy(spawned[i].gameObject);
        }

        spawned.Clear();
    }
}
