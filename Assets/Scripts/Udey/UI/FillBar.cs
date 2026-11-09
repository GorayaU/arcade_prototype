using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// Shared behaviour for every resource bar in the game: a filled Image, a slower "trail"
/// bar behind it that makes a drop readable, colour that shifts as the value falls, and
/// an opacity pulse when things are critical.

/// Subclasses only have to answer "where does the number come from". See StaminaBar and HealthBar.

/// Setup:
///   1. UI > Image, name it e.g. "HealthBackground". Dark colour, sized how you want the bar.
///   2. A child Image "…Fill", stretched over the parent.
///   3. The fill Image NEEDS a Source Image (any sprite, e.g. the built-in "UISprite").
///      A Filled Image with Source Image = None ignores fillAmount and never moves.
///   4. Put the bar script on the background and assign the fill.
/// The trail bar and the CanvasGroup are created at runtime if you leave them empty.

public abstract class FillBar : MonoBehaviour
{
    public enum FillDirection { Vertical, Horizontal }

    [Header("Bar images")]
    [Tooltip("Tracks the real value. Needs a Source Image assigned.")]
    [SerializeField] protected Image fillImage;

    [Tooltip("Sits BEHIND the fill and catches up slowly. Built automatically if empty.")]
    [SerializeField] protected Image trailImage;

    [Tooltip("Used for fading and the critical pulse. Added automatically if empty.")]
    [SerializeField] protected CanvasGroup canvasGroup;

    [SerializeField] private bool autoCreateTrail = true;

    [Header("Direction")]
    [Tooltip("Vertical = the level slides down as it empties. Horizontal = classic side bar.")]
    [SerializeField] protected FillDirection direction = FillDirection.Vertical;

    [Tooltip("Flip which end the bar drains toward.")]
    [SerializeField] protected bool reverseOrigin = false;

    [Header("Feel")]
    [Tooltip("How fast the main fill chases the real value. 0 = instant, which reads best " +
             "when the trail bar is doing the lagging.")]
    [SerializeField] private float fillSmoothing = 0f;

    [Tooltip("Seconds the trail holds still after a drop before catching up.")]
    [SerializeField] private float trailDelay = 0.3f;

    [Tooltip("How fast the trail catches up, as a fraction of the full bar per second.")]
    [SerializeField] private float trailSpeed = 0.55f;

    [Header("Visibility")]
    [Tooltip("Fade the bar out when the value is full and nothing is happening.")]
    [SerializeField] protected bool hideWhenFull = false;

    [SerializeField] private float fadeSpeed = 4f;

    [Tooltip("Pulse the bar's opacity while the value is critical.")]
    [FormerlySerializedAs("pulseWhenExhausted")]
    [SerializeField] private bool pulseWhenCritical = true;

    [SerializeField] private float pulseSpeed = 6f;

    [Header("Colours")]
    [SerializeField] private Color normalColor = new Color(0.30f, 0.80f, 0.45f);
    [SerializeField] private Color lowColor = new Color(0.95f, 0.75f, 0.25f);

    [FormerlySerializedAs("exhaustedColor")]
    [SerializeField] private Color criticalColor = new Color(0.85f, 0.25f, 0.25f);

    [Tooltip("Colour of the chunk that's currently draining away.")]
    [SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0.65f);

    [Tooltip("Below this fraction the bar starts blending toward the low colour.")]
    [Range(0f, 1f)][SerializeField] private float lowThreshold = 0.4f;

    private float displayedValue = 1f;
    private float trailValue = 1f;
    private float trailHoldTimer;

    // ---------------------------------------------------------------- subclass contract

    /// Find whatever this bar reads from. Return false to disable the bar.
    protected abstract bool ResolveSource();

    /// The value to show, 0 to 1.
    protected abstract float GetTargetValue01();

    /// Paint the bar in the critical colour and pulse it.
    protected virtual bool IsCritical => false;

    /// Keep the bar on screen even at full, e.g. while sprinting.
    protected virtual bool ForceVisible => false;

    // ---------------------------------------------------------------- lifecycle

    protected virtual void Awake()
    {
        if (!ResolveSource() || fillImage == null)
        {
            if (fillImage == null) Debug.LogError($"[{GetType().Name}] No Fill Image assigned.", this);
            enabled = false;
            return;
        }

        CreateTrailIfMissing();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        ConfigureImage(fillImage);
        ConfigureImage(trailImage);
        if (trailImage != null) trailImage.color = trailColor;

        displayedValue = GetTargetValue01();
        trailValue = displayedValue;
    }

    protected virtual void Update()
    {
        float dt = Time.deltaTime;
        float target = Mathf.Clamp01(GetTargetValue01());

        displayedValue = fillSmoothing <= 0f
            ? target
            : Mathf.Lerp(displayedValue, target, 1f - Mathf.Exp(-fillSmoothing * dt));

        fillImage.fillAmount = displayedValue;
        fillImage.color = GetColor(target);

        UpdateTrail(dt);
        UpdateAlpha(target, dt);
    }

    // ---------------------------------------------------------------- internals
    
    /// Clones the fill Image to make the trail bar, and puts the clone earlier in the
    /// sibling order so it renders BEHIND the fill.

    private void CreateTrailIfMissing()
    {
        if (trailImage != null || !autoCreateTrail) return;
        if (fillImage.transform.parent == null) return;

        GameObject clone = Instantiate(fillImage.gameObject, fillImage.transform.parent);
        clone.name = fillImage.name + " Trail (auto)";

        // Strip any scripts that came along, but keep the Image itself.
        foreach (MonoBehaviour behaviour in clone.GetComponents<MonoBehaviour>())
        {
            if (!(behaviour is Graphic)) Destroy(behaviour);
        }

        trailImage = clone.GetComponent<Image>();
        clone.transform.SetSiblingIndex(fillImage.transform.GetSiblingIndex());
    }

    /// Forces an Image into the right Filled mode and warns about the missing-sprite trap.
    protected void ConfigureImage(Image image, bool warn = true)
    {
        if (image == null) return;

        if (warn && image.sprite == null)
        {
            Debug.LogError(
                $"[{GetType().Name}] '{image.name}' has no Source Image, so it renders as a solid " +
                "block and IGNORES fillAmount — the bar will never move. Set Source Image to any " +
                "sprite, e.g. the built-in 'UISprite'.", image);
        }

        image.type = Image.Type.Filled;

        if (direction == FillDirection.Vertical)
        {
            image.fillMethod = Image.FillMethod.Vertical;
            // Origin Bottom means the top surface slides down as the value drops.
            image.fillOrigin = (int)(reverseOrigin ? Image.OriginVertical.Top : Image.OriginVertical.Bottom);
        }
        else
        {
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)(reverseOrigin ? Image.OriginHorizontal.Right : Image.OriginHorizontal.Left);
        }
    }

    private void UpdateTrail(float dt)
    {
        if (trailImage == null) return;

        if (displayedValue < trailValue)
        {
            // Losing: hold the trail, then let it slide down to meet the fill.
            trailHoldTimer += dt;
            if (trailHoldTimer >= trailDelay)
            {
                trailValue = Mathf.MoveTowards(trailValue, displayedValue, trailSpeed * dt);
            }
        }
        else
        {
            // Gaining: the trail rides the fill, so a heal reads as one clean bar.
            trailValue = displayedValue;
            trailHoldTimer = 0f;
        }

        trailImage.fillAmount = trailValue;
    }

    private Color GetColor(float normalized)
    {
        if (IsCritical) return criticalColor;
        if (lowThreshold <= 0f) return normalColor;

        float t = Mathf.InverseLerp(lowThreshold, 0f, normalized);
        return Color.Lerp(normalColor, lowColor, t);
    }

    private void UpdateAlpha(float normalized, float dt)
    {
        if (canvasGroup == null) return;

        if (pulseWhenCritical && IsCritical)
        {
            // Sine between ~0.55 and 1.0 so it throbs without disappearing.
            canvasGroup.alpha = 0.775f + Mathf.Sin(Time.time * pulseSpeed) * 0.225f;
            return;
        }

        bool shouldShow = !hideWhenFull || normalized < 0.999f || ForceVisible;
        float targetAlpha = shouldShow ? 1f : 0f;

        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * dt);
    }

#if UNITY_EDITOR

    /// Right-click the component header > "Preview: Half Empty" while NOT playing.
    /// If the bar doesn't drop to half, the problem is the Image setup (almost always a
    /// missing Source Image), not the game logic.
    
    [ContextMenu("Preview: Half Empty")]
    private void PreviewHalfEmpty()
    {
        ConfigureImage(fillImage, warn: false);
        if (fillImage != null) fillImage.fillAmount = 0.5f;
        if (trailImage != null) trailImage.fillAmount = 0.8f;
    }

    [ContextMenu("Preview: Full")]
    private void PreviewFull()
    {
        ConfigureImage(fillImage, warn: false);
        if (fillImage != null) fillImage.fillAmount = 1f;
        if (trailImage != null) trailImage.fillAmount = 1f;
    }

    // Re-apply fill method/origin live when Direction changes in the inspector.
    protected virtual void OnValidate()
    {
        ConfigureImage(fillImage, warn: false);
        ConfigureImage(trailImage, warn: false);
        if (trailImage != null) trailImage.color = trailColor;
    }
#endif
}
