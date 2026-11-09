using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One line of the leaderboard. Make a prefab out of a horizontal row of TMP texts,
/// put this on its root, and assign whichever fields you actually use.
///
/// LeaderboardUI clones this prefab once per entry — nothing calls it directly.
/// </summary>
public class LeaderboardRow : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;

    [Tooltip("Secondary line: difficulty, wave and date.")]
    [SerializeField] private TMP_Text detailText;

    [Header("Highlight")]
    [Tooltip("Background tinted when this row is the run that just finished.")]
    [SerializeField] private Image background;

    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.05f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.3f, 0.25f);

    /// <summary>Fills the row in. Called by LeaderboardUI.</summary>
    public void Bind(int rank, ScoreEntry entry, bool highlight)
    {
        if (entry == null) return;

        if (rankText != null) rankText.text = rank.ToString();
        if (nameText != null) nameText.text = entry.playerName;
        if (scoreText != null) scoreText.text = entry.score.ToString("N0");

        if (detailText != null)
        {
            // "Hard · Wave 7 · 42 kills"
            detailText.text = $"{entry.difficulty} · Wave {entry.wave} · {entry.kills} kills";
        }

        if (background != null) background.color = highlight ? highlightColor : normalColor;
    }
}
