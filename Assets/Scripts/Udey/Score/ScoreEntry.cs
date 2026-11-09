using System;

/// <summary>
/// One finished run on the leaderboard. Plain [Serializable] class, not a struct and not a
/// MonoBehaviour, because JsonUtility has to be able to write it to PlayerPrefs.
/// </summary>
[Serializable]
public class ScoreEntry
{
    public string playerName = "Player";
    public int score;
    public int wave;
    public int kills;

    /// <summary>"Story", "Easy", "Medium" or "Hard" — stored as text so old saves survive
    /// you renaming or reordering the Difficulty enum.</summary>
    public string difficulty = "Medium";

    /// <summary>ISO date, so rows can be sorted or shown without a DateTime round-trip.</summary>
    public string dateIso = "";

    public ScoreEntry() { }

    public ScoreEntry(string playerName, int score, int wave, int kills, string difficulty)
    {
        this.playerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
        this.score = score;
        this.wave = wave;
        this.kills = kills;
        this.difficulty = difficulty;
        dateIso = DateTime.Now.ToString("yyyy-MM-dd");
    }
}
