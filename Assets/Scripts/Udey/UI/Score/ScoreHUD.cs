using TMPro;
using UnityEngine;
using Udey.Core;

/// <summary>
/// Live score readout. Put it on your HUD Canvas next to AmmoHUD.
///
/// The number counts up to its target instead of snapping, because a score that ticks
/// reads as a reward and a score that jumps reads as a typo. Every field is optional —
/// wire up only the ones your HUD has.
/// </summary>
public class ScoreHUD : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text killsText;

    [Tooltip("Shows the difficulty multiplier, e.g. 'HARD x1.5'. Hidden on Story / x1.")]
    [SerializeField] private TMP_Text multiplierText;

    [Header("Formatting")]
    [Tooltip("Thousands separators. 12,400 reads faster than 12400.")]
    [SerializeField] private bool useThousandsSeparator = true;

    [SerializeField] private string scorePrefix = "";
    [SerializeField] private string killsFormat = "Kills: {0}";

    [Header("Count-up")]
    [Tooltip("Points per second the display chases the real score. 0 snaps instantly.")]
    [SerializeField] private float countUpSpeed = 900f;

    [Tooltip("Flash colour when points come in. Set alpha to 0 to disable the flash.")]
    [SerializeField] private Color gainFlashColor = new Color(1f, 0.9f, 0.3f, 1f);

    [SerializeField] private float flashFadeSpeed = 4f;

    private ScoreManager manager;
    private Color baseColor = Color.white;
    private float displayed;
    private float flash;

    private void Start()
    {
        manager = ScoreManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("[ScoreHUD] No ScoreManager in the scene — the score won't update.", this);
            enabled = false;
            return;
        }

        if (scoreText != null) baseColor = scoreText.color;

        manager.OnScoreChanged += HandleScoreChanged;

        displayed = manager.Score;
        RedrawScore();
        RedrawKills();
        RedrawMultiplier();
    }

    private void OnDestroy()
    {
        if (manager != null) manager.OnScoreChanged -= HandleScoreChanged;
    }

    private void Update()
    {
        float target = manager.Score;

        if (!Mathf.Approximately(displayed, target))
        {
            displayed = countUpSpeed <= 0f
                ? target
                // Scale the speed with the gap so a 4000-point Tank kill doesn't take
                // four times as long to tick up as a 1000-point one.
                : Mathf.MoveTowards(displayed, target,
                    Mathf.Max(countUpSpeed, Mathf.Abs(target - displayed) * 2f) * Time.deltaTime);

            RedrawScore();
        }

        if (flash > 0f && scoreText != null)
        {
            flash = Mathf.MoveTowards(flash, 0f, flashFadeSpeed * Time.deltaTime);
            scoreText.color = Color.Lerp(baseColor, gainFlashColor, flash * gainFlashColor.a);
        }
    }

    private void HandleScoreChanged(int total, int delta)
    {
        if (delta > 0) flash = 1f;
        RedrawKills();
    }

    // ---------------------------------------------------------------- drawing

    private void RedrawScore()
    {
        if (scoreText == null) return;

        int shown = Mathf.RoundToInt(displayed);
        scoreText.text = scorePrefix + (useThousandsSeparator ? shown.ToString("N0") : shown.ToString());
    }

    private void RedrawKills()
    {
        if (killsText == null) return;
        killsText.text = string.Format(killsFormat, manager.Kills);
    }

    private void RedrawMultiplier()
    {
        if (multiplierText == null) return;

        float m = manager.Multiplier;

        // Nothing useful to say when the multiplier is 1.
        if (Mathf.Approximately(m, 1f))
        {
            multiplierText.gameObject.SetActive(false);
            return;
        }

        multiplierText.gameObject.SetActive(true);
        multiplierText.text = $"{GameSession.Label.ToUpper()}  x{m:0.##}";
    }
}
