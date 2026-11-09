using System;
using UnityEngine;
using Udey.Core;

/// <summary>
/// The running score for the current attempt. Put it on the GameManager object next to
/// WaveManager — one per gameplay scene, reset automatically when the scene loads.
///
/// Enemies don't talk to this directly; ScoreOnDeath does, through the static Award helper.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("Used to record which wave the run ended on. Found automatically if empty.")]
    [SerializeField] private WaveManager waveManager;

    [Header("Submission")]
    [Tooltip("Save the run to the leaderboard automatically when the player dies.")]
    [SerializeField] private bool submitOnPlayerDeath = true;

    [Header("Debug")]
    [SerializeField] private bool logScoring = false;

    // --- State ---

    public int Score { get; private set; }
    public int Kills { get; private set; }

    /// <summary>The difficulty multiplier every award is run through.</summary>
    public float Multiplier { get { return GameSession.ScoreMultiplier; } }

    public int CurrentWave { get { return waveManager != null ? waveManager.CurrentWaveNumber : 0; } }

    /// <summary>(new total, points just added)</summary>
    public event Action<int, int> OnScoreChanged;

    /// <summary>(the saved entry, its 1-based rank or -1) — fires when a run is recorded.</summary>
    public event Action<ScoreEntry, int> OnRunSubmitted;

    private PlayerHealth playerHealth;
    private bool submitted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ScoreManager] A second ScoreManager appeared. Keeping the first.", this);
            return;
        }

        Instance = this;
        Score = 0;
        Kills = 0;
        submitted = false;

        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
    }

    private void Start()
    {
        if (!submitOnPlayerDeath) return;

        PlayerRig rig = PlayerRig.Instance;
        if (rig == null || rig.PlayerHealth == null)
        {
            Debug.LogWarning("[ScoreManager] No PlayerHealth found, so runs won't be saved " +
                             "automatically. Call SubmitRun() yourself from your game-over screen.", this);
            return;
        }

        playerHealth = rig.PlayerHealth;
        playerHealth.OnDied += HandlePlayerDied;
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDied;
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------- awarding

    /// <summary>
    /// Adds points with the difficulty multiplier applied. This is what kills go through,
    /// so Hard runs are genuinely worth more than Easy ones.
    /// </summary>
    public void AddPoints(int basePoints)
    {
        if (basePoints <= 0) return;

        int awarded = Mathf.RoundToInt(basePoints * Multiplier);
        if (awarded <= 0) awarded = 1; // never silently award nothing

        Score += awarded;
        OnScoreChanged?.Invoke(Score, awarded);

        if (logScoring)
        {
            Debug.Log($"[ScoreManager] +{awarded} ({basePoints} x{Multiplier:0.00}) = {Score}");
        }
    }

    /// <summary>Adds points with NO multiplier — for flat bonuses you've already balanced.</summary>
    public void AddRawPoints(int points)
    {
        if (points == 0) return;

        Score = Mathf.Max(0, Score + points);
        OnScoreChanged?.Invoke(Score, points);
    }

    /// <summary>Called by ScoreOnDeath. Counts the kill and awards the points.</summary>
    public void AwardKill(int basePoints)
    {
        Kills++;
        AddPoints(basePoints);
    }

    /// <summary>
    /// Safe from anywhere — does nothing if there's no ScoreManager in the scene, so a
    /// test scene without one doesn't throw a null reference on every kill.
    /// </summary>
    public static void Award(int basePoints)
    {
        if (Instance != null) Instance.AwardKill(basePoints);
    }

    // ---------------------------------------------------------------- finishing

    private void HandlePlayerDied()
    {
        SubmitRun();
    }

    /// <summary>
    /// Writes the run to the leaderboard. Safe to call twice — only the first one counts,
    /// so a game-over screen and the death event can't double-submit.
    /// </summary>
    public ScoreEntry SubmitRun()
    {
        if (submitted) return null;
        submitted = true;

        ScoreEntry entry = new ScoreEntry(
            Leaderboard.PlayerName, Score, CurrentWave, Kills, GameSession.Label);

        int rank = Leaderboard.Submit(entry);

        if (logScoring) Debug.Log($"[ScoreManager] Run saved: {Score} pts, rank {rank}");

        OnRunSubmitted?.Invoke(entry, rank);
        return entry;
    }

    /// <summary>Starts a fresh attempt without reloading the scene.</summary>
    public void ResetRun()
    {
        Score = 0;
        Kills = 0;
        submitted = false;
        OnScoreChanged?.Invoke(0, 0);
    }
}
