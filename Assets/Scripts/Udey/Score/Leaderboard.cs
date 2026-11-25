using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The saved high score table. Static, with no scene presence — anything can read or write
/// it from anywhere, including the main menu scene where no gameplay objects exist.
///
/// Stored in PlayerPrefs as one JSON string. That's deliberate: PlayerPrefs works
/// identically in the editor, in a build, and in WebGL, with no file permissions to worry
/// about. A table of ten rows is far too small for the size limit to matter.
/// </summary>
public static class Leaderboard
{
    /// <summary>How many rows the table keeps. Anything below this is dropped on submit.</summary>
    public const int MaxEntries = 10;

    private const string TableKey = "udey.leaderboard.v1";
    private const string NameKey = "udey.playername";

    /// <summary>JsonUtility can't serialise a bare List, so it needs a wrapper object.</summary>
    [Serializable]
    private class Table
    {
        public List<ScoreEntry> entries = new List<ScoreEntry>();
    }

    /// <summary>Fires after any change, so open leaderboard UI can refresh itself.</summary>
    public static event Action OnChanged;

    /// <summary>Remembered between runs, so the player types their name once.</summary>
    public static string PlayerName
    {
        get { return PlayerPrefs.GetString(NameKey, "Player"); }
        set
        {
            string clean = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();
            PlayerPrefs.SetString(NameKey, clean);
            PlayerPrefs.Save();
        }
    }

    // ---------------------------------------------------------------- reading

    /// <summary>Every saved run, best first.</summary>
    public static List<ScoreEntry> GetAll()
    {
        return Load().entries;
    }

    /// <summary>
    /// Best first, capped at <paramref name="count"/>.
    /// Pass a difficulty ("Hard") to show only that board, or null for all runs.
    /// </summary>
    public static List<ScoreEntry> GetTop(int count, string difficultyFilter = null)
    {
        List<ScoreEntry> all = Load().entries;
        List<ScoreEntry> result = new List<ScoreEntry>();

        for (int i = 0; i < all.Count && result.Count < count; i++)
        {
            if (!string.IsNullOrEmpty(difficultyFilter) && all[i].difficulty != difficultyFilter) continue;
            result.Add(all[i]);
        }

        return result;
    }

    /// <summary>True if this score would make the table — for a "NEW HIGH SCORE" banner.</summary>
    public static bool Qualifies(int score)
    {
        if (score <= 0) return false;

        List<ScoreEntry> all = Load().entries;
        if (all.Count < MaxEntries) return true;

        return score > all[all.Count - 1].score;
    }

    public static int BestScore
    {
        get
        {
            List<ScoreEntry> all = Load().entries;
            return all.Count > 0 ? all[0].score : 0;
        }
    }

    // ---------------------------------------------------------------- writing

    /// <summary>
    /// Saves a run and returns its 1-based rank, or -1 if it didn't make the table.
    /// </summary>
    public static int Submit(ScoreEntry entry)
    {
        if (entry == null || entry.score <= 0) return -1;

        Table table = Load();
        table.entries.Add(entry);

        // Highest first; ties broken by the deeper wave, so a longer run wins.
        table.entries.Sort(delegate (ScoreEntry a, ScoreEntry b)
        {
            int byScore = b.score.CompareTo(a.score);
            return byScore != 0 ? byScore : b.wave.CompareTo(a.wave);
        });

        if (table.entries.Count > MaxEntries)
        {
            table.entries.RemoveRange(MaxEntries, table.entries.Count - MaxEntries);
        }

        Save(table);

        int rank = table.entries.IndexOf(entry);
        return rank >= 0 ? rank + 1 : -1;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(TableKey);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    // ---------------------------------------------------------------- storage

    private static Table Load()
    {
        string json = PlayerPrefs.GetString(TableKey, "");
        if (string.IsNullOrEmpty(json)) return new Table();

        Table table = null;
        try
        {
            table = JsonUtility.FromJson<Table>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Leaderboard] Saved table was unreadable, starting fresh. " + e.Message);
        }

        if (table == null) return new Table();
        if (table.entries == null) table.entries = new List<ScoreEntry>();
        return table;
    }

    private static void Save(Table table)
    {
        PlayerPrefs.SetString(TableKey, JsonUtility.ToJson(table));
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}
