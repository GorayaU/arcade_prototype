using UnityEngine;

/// Stamina bar. All the drawing lives in FillBar — this just says where the number
/// comes from and when the bar should go red.

/// Your existing inspector values carry over: the shared fields moved up into FillBar,
/// but Unity serialises by field name, so Fill Image, Direction, colours and the rest
/// are all still set.

public class StaminaBar : FillBar
{
    [Header("Source")]
    [SerializeField] private PlayerMovement player;

    [Header("Debug")]
    [Tooltip("Print stamina to the Console once a second. If the numbers move but the bar " +
             "doesn't, the problem is the Image, not the stamina logic.")]
    [SerializeField] private bool logStaminaToConsole = false;

    private float logTimer;

    protected override bool ResolveSource()
    {
        if (player == null) player = FindFirstObjectByType<PlayerMovement>();

        if (player == null)
        {
            Debug.LogError("[StaminaBar] No PlayerMovement assigned or found in the scene.", this);
            return false;
        }

        return true;
    }

    protected override float GetTargetValue01() => player.StaminaNormalized;

    protected override bool IsCritical => player.IsExhausted;

    protected override bool ForceVisible => player.IsSprinting;

    protected override void Update()
    {
        base.Update();

        if (!logStaminaToConsole) return;

        logTimer += Time.deltaTime;
        if (logTimer < 1f) return;

        logTimer = 0f;
        Debug.Log($"[StaminaBar] stamina {player.StaminaNormalized:P0} | " +
                  $"sprinting {player.IsSprinting} | exhausted {player.IsExhausted}");
    }
}
