using UnityEngine;
using UnityEngine.UI;

namespace Udey
{
    /// <summary>
    /// Drives a uGUI Image from PlayerMovement's stamina.
    ///
    /// By default the bar is VERTICAL and drains downward, like a liquid level dropping,
    /// and a slower "trail" bar lags behind so you can see exactly how much you just spent.
    ///
    /// Setup (vertical bar):
    ///   1. UI > Image, name it "StaminaBackground". Make it tall and narrow (e.g. 40 x 200).
    ///   2. Duplicate it twice inside it as children, stretched to fill the parent:
    ///        StaminaBackground
    ///          |- StaminaTrail   (drawn first = behind)
    ///          |- StaminaFill    (drawn last  = in front)
    ///   3. IMPORTANT: both child Images need a Source Image assigned (any sprite, e.g. the
    ///      built-in "UISprite"). An Image with Source Image = None ignores fillAmount entirely.
    ///   4. Put this script on StaminaBackground, assign Player, Fill Image and Trail Image.
    ///      Fill method / origin are set from Direction automatically.
    /// </summary>
    public class StaminaBar : MonoBehaviour
    {
        public enum FillDirection { Vertical, Horizontal }

        [Header("References")]
        [SerializeField] private PlayerMovement player;

        [Tooltip("The bar that tracks stamina exactly. Needs a Source Image assigned.")]
        [SerializeField] private Image fillImage;

        [Tooltip("Optional. Sits BEHIND the fill and catches up slowly, so a drain reads as a " +
                 "visible chunk draining away. Leave empty to skip it.")]
        [SerializeField] private Image trailImage;

        [Tooltip("Optional. Add a CanvasGroup to this object for fading and the low-stamina pulse.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Direction")]
        [Tooltip("Vertical = the level slides down as it empties. Horizontal = classic side bar.")]
        [SerializeField] private FillDirection direction = FillDirection.Vertical;

        [Tooltip("Flip which end the bar drains toward (top-down / right-to-left).")]
        [SerializeField] private bool reverseOrigin = false;

        [Header("Feel")]
        [Tooltip("How fast the main fill chases the real value. 0 = instant, which reads best " +
                 "when a trail bar is doing the lagging for you.")]
        [SerializeField] private float fillSmoothing = 0f;

        [Tooltip("Seconds the trail waits after stamina drops before it starts catching up.")]
        [SerializeField] private float trailDelay = 0.3f;

        [Tooltip("How fast the trail catches up, as a fraction of the full bar per second.")]
        [SerializeField] private float trailSpeed = 0.55f;

        [Header("Visibility")]
        [Tooltip("Fade the bar out when stamina is full and the player isn't sprinting.")]
        [SerializeField] private bool hideWhenFull = true;

        [SerializeField] private float fadeSpeed = 4f;

        [Tooltip("Pulse the bar's opacity while exhausted, so an empty bar is impossible to miss.")]
        [SerializeField] private bool pulseWhenExhausted = true;

        [SerializeField] private float pulseSpeed = 6f;

        [Header("Colours")]
        [SerializeField] private Color normalColor = new Color(0.30f, 0.80f, 0.45f);
        [SerializeField] private Color lowColor = new Color(0.95f, 0.75f, 0.25f);
        [SerializeField] private Color exhaustedColor = new Color(0.85f, 0.25f, 0.25f);

        [Tooltip("Colour of the chunk that's currently draining away.")]
        [SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0.65f);

        [Tooltip("Below this fraction the bar starts blending toward the low colour.")]
        [Range(0f, 1f)][SerializeField] private float lowThreshold = 0.4f;

        private float displayedValue = 1f;
        private float trailValue = 1f;
        private float trailHoldTimer;

        private void Awake()
        {
            if (player == null) player = FindFirstObjectByType<PlayerMovement>();

            if (player == null || fillImage == null)
            {
                Debug.LogError("[StaminaBar] Assign both Player and Fill Image in the inspector.", this);
                enabled = false;
                return;
            }

            ConfigureImage(fillImage);
            ConfigureImage(trailImage);

            if (trailImage != null) trailImage.color = trailColor;

            displayedValue = player.StaminaNormalized;
            trailValue = displayedValue;
        }

        /// <summary>Forces an Image into the right Filled mode and warns about the missing-sprite trap.</summary>
        private void ConfigureImage(Image image, bool warn = true)
        {
            if (image == null) return;

            if (warn && image.sprite == null)
            {
                Debug.LogWarning(
                    $"[StaminaBar] '{image.name}' has no Source Image. A Filled Image with no sprite " +
                    "ignores fillAmount and will render as a solid block. Assign any sprite (e.g. UISprite).",
                    image);
            }

            image.type = Image.Type.Filled;

            if (direction == FillDirection.Vertical)
            {
                image.fillMethod = Image.FillMethod.Vertical;
                // Origin Bottom means the filled part grows up from the bottom, so the top
                // surface slides DOWN as stamina drains.
                image.fillOrigin = (int)(reverseOrigin ? Image.OriginVertical.Top : Image.OriginVertical.Bottom);
            }
            else
            {
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)(reverseOrigin ? Image.OriginHorizontal.Right : Image.OriginHorizontal.Left);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float target = player.StaminaNormalized;

            UpdateFill(target, dt);
            UpdateTrail(dt);
            UpdateAlpha(target, dt);
        }

        private void UpdateFill(float target, float dt)
        {
            displayedValue = fillSmoothing <= 0f
                ? target
                : Mathf.Lerp(displayedValue, target, 1f - Mathf.Exp(-fillSmoothing * dt));

            fillImage.fillAmount = displayedValue;
            fillImage.color = GetColor(target);
        }

        private void UpdateTrail(float dt)
        {
            if (trailImage == null) return;

            if (displayedValue < trailValue)
            {
                // Losing stamina: hold the trail where it was, then let it slide down to meet the fill.
                trailHoldTimer += dt;
                if (trailHoldTimer >= trailDelay)
                {
                    trailValue = Mathf.MoveTowards(trailValue, displayedValue, trailSpeed * dt);
                }
            }
            else
            {
                // Gaining stamina: the trail rides the fill exactly, so regen reads as one clean bar.
                trailValue = displayedValue;
                trailHoldTimer = 0f;
            }

            trailImage.fillAmount = trailValue;
        }

        private Color GetColor(float normalized)
        {
            if (player.IsExhausted) return exhaustedColor;
            if (lowThreshold <= 0f) return normalColor;

            // Blend normal -> low as stamina falls through the threshold.
            float t = Mathf.InverseLerp(lowThreshold, 0f, normalized);
            return Color.Lerp(normalColor, lowColor, t);
        }

        private void UpdateAlpha(float normalized, float dt)
        {
            if (canvasGroup == null) return;

            bool shouldShow = !hideWhenFull
                              || normalized < 0.999f
                              || player.IsSprinting
                              || player.IsExhausted;

            float targetAlpha = shouldShow ? 1f : 0f;

            if (pulseWhenExhausted && player.IsExhausted)
            {
                // Sine between ~0.55 and 1.0 so it throbs without disappearing.
                targetAlpha = 0.775f + Mathf.Sin(Time.time * pulseSpeed) * 0.225f;
                canvasGroup.alpha = targetAlpha;
                return;
            }

            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * dt);
        }

#if UNITY_EDITOR
        // Re-apply fill method/origin live when you change Direction in the inspector.
        private void OnValidate()
        {
            ConfigureImage(fillImage, warn: false);
            ConfigureImage(trailImage, warn: false);
            if (trailImage != null) trailImage.color = trailColor;
        }
#endif
    }
}