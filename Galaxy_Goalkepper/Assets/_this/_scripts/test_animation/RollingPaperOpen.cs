using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Drop on any GameObject (or on the Image itself — Target auto-fills), and the
// rolling-paper open plays automatically every time the GameObject is enabled.
// Call PlayUnroll() to re-trigger manually, ResetToRolled() to snap closed,
// SnapToOpen() to snap fully open.
//
// Visual: the pivot is moved to the rolling end (no visual jump thanks to
// SetPivotPreservePosition), localScale on the rolling axis lerps from
// `startScale` to 1 with an EaseOutBack overshoot so the leading edge "drops
// and settles" like a real paper. A damped sine sway on Z-rotation layers in
// the inertia wobble so it doesn't feel like a clinical wipe.
[DisallowMultipleComponent]
public class RollingPaperOpen : MonoBehaviour
{
    public enum UnrollDirection { TopToBottom, BottomToTop, LeftToRight, RightToLeft }

    [Header("Target")]
    [Tooltip("The Image to animate. Auto-uses this GameObject's Image if left empty.")]
    public Image target;

    [Header("Unroll")]
    [Tooltip("Which way the paper opens. TopToBottom is the classic scroll unfurl.")]
    public UnrollDirection direction = UnrollDirection.TopToBottom;
    [Tooltip("Total time, in seconds, for the unroll.")]
    public float unrollDuration = 1.2f;
    [Tooltip("Starting scale on the rolling axis. 0 = invisible start (clean reveal); 0.02-0.05 = thin visible 'rolled strip' (more roll-like with textured images).")]
    [Range(0f, 0.2f)] public float startScale = 0f;
    [Tooltip("End-of-unroll overshoot for a paper-settle bounce. 0 = ease-out-cubic; ~0.15 = pronounced bounce.")]
    [Range(0f, 0.5f)] public float endBounce = 0.15f;

    [Header("Sway (paper feel)")]
    [Tooltip("Damped Z-rotation oscillation across the unroll. 0 = no sway. 2-5 = subtle paper wobble.")]
    [Range(0f, 15f)] public float swayDegrees = 3f;
    [Tooltip("Number of full sway oscillations across the unroll duration.")]
    [Range(0f, 5f)] public float swayCycles = 1.2f;

    [Header("Trigger")]
    [Tooltip("If true, the unroll animation runs every time this GameObject is enabled.")]
    public bool autoPlayOnEnable = true;
    [Tooltip("Use Time.unscaledDeltaTime so the unroll plays even when Time.timeScale = 0.")]
    public bool useUnscaledTime = true;

    public LeaderboardController leaderboard;
    public GameObject go;

    [Header("Events")]
    [Tooltip("Fires when the unroll animation reaches its fully-open state (also fires from SnapToOpen). Does NOT fire when an in-flight animation is interrupted by another PlayUnroll/ResetToRolled call.")]
    public UnityEvent onUnrollComplete;

    private RectTransform rt;
    private Coroutine running;
    private Vector2 originalPivot;
    private Vector3 originalLocalScale;
    private Quaternion originalLocalRot;
    private bool captured;
    // True while HandleUnrollComplete is running. Any OnEnable that fires during that window
    // (e.g. because enabling the leaderboard toggled our GameObject off/on, or because
    // go.SetActive(true) re-fired our OnEnable) is skipped — otherwise the paper would
    // instantly collapse back to rolled state and "disappear" right after opening.
    private bool suppressOnEnable;

    private void Awake()
    {
        if (target == null) target = GetComponent<Image>();
        if (target == null) return;
        rt = target.rectTransform;
        CaptureOriginal();
        // Always stage in the rolled-up state during Awake so the image is collapsed from
        // the very first render frame and stays rolled until PlayUnroll() is called
        // (either by autoPlayOnEnable below, or by an external trigger later).
        StageRolled();
    }

    private void OnEnable()
    {
        // If we just programmatically toggled our own GameObject (via leaderboard.enabled
        // or go.SetActive in HandleUnrollComplete), skip the reset so the paper stays at
        // fully-open. Without this guard the paper visibly snaps back to rolled.
        if (suppressOnEnable) return;
        if (leaderboard != null) leaderboard.enabled = false;
        if (!EnsureReady()) return;
        // Snap back to rolled on every enable cycle so toggling the GameObject off/on
        // always returns to the collapsed state before any unroll trigger fires.
        StageRolled();
        if (autoPlayOnEnable) PlayUnroll();
    }

    // -------------------- Public API --------------------

    // Triggers the unroll: rolled (collapsed) -> fully open.
    public void PlayUnroll()
    {
        if (!EnsureReady()) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(UnrollCo());
    }

    // Snaps to the rolled-up (collapsed) state without animating.
    public void ResetToRolled()
    {
        if (!EnsureReady()) return;
        if (running != null) { StopCoroutine(running); running = null; }
        StageRolled();
    }

    // Snaps to fully open without animating.
    public void SnapToOpen()
    {
        if (!EnsureReady()) return;
        if (running != null) { StopCoroutine(running); running = null; }
        ApplyPivotForDirection();
        SetUnrollProgress(1f);
        rt.localRotation = originalLocalRot;
        HandleUnrollComplete();
    }

    // -------------------- Internals --------------------

    private bool EnsureReady()
    {
        if (target == null) target = GetComponent<Image>();
        if (target == null)
        {
            Debug.LogWarning("[RollingPaperOpen] No target Image assigned and no Image on this GameObject.");
            return false;
        }
        if (rt == null) rt = target.rectTransform;
        if (!captured) CaptureOriginal();
        return rt != null;
    }

    private void CaptureOriginal()
    {
        originalPivot = rt.pivot;
        // Defend against capturing a degenerate scale (e.g. a previous run left scale=0 on disable).
        Vector3 s = rt.localScale;
        if (Mathf.Abs(s.x) < 0.0001f) s.x = 1f;
        if (Mathf.Abs(s.y) < 0.0001f) s.y = 1f;
        if (Mathf.Abs(s.z) < 0.0001f) s.z = 1f;
        originalLocalScale = s;
        originalLocalRot = rt.localRotation;
        captured = true;
    }

    private void StageRolled()
    {
        ApplyPivotForDirection();
        SetUnrollProgress(Mathf.Max(0f, startScale));
        rt.localRotation = originalLocalRot;
    }

    private IEnumerator UnrollCo()
    {
        StageRolled();

        float dur = Mathf.Max(0.001f, unrollDuration);
        float t = 0f;
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float eased = EaseOutBack(u, endBounce);
            // Lerp the visible scale from startScale -> 1, allowing the back-ease to overshoot past 1.
            float progress = Mathf.LerpUnclamped(startScale, 1f, eased);
            SetUnrollProgress(progress);

            if (swayDegrees > 0.001f)
            {
                // Damped sine: oscillates while u < 1, ringing out as u approaches 1 so the
                // paper settles still at the end instead of stopping mid-swing.
                float swayU = Mathf.Sin(u * Mathf.PI * swayCycles * 2f) * (1f - u);
                rt.localRotation = originalLocalRot * Quaternion.Euler(0f, 0f, swayU * swayDegrees);
            }
            yield return null;
        }
        SetUnrollProgress(1f);
        rt.localRotation = originalLocalRot;
        running = null;
        HandleUnrollComplete();
    }

    // Re-enables paired components (leaderboard) and fires the public completion event.
    // Called once per animation, only when the animation actually reaches the fully-open state
    // (natural end of UnrollCo or an explicit SnapToOpen). Interrupted animations skip this.
    //
    // While this runs, suppressOnEnable is held true so the GameObject toggles caused by
    // enabling the leaderboard (which calls SetChildrenVisible(false)→SetChildrenVisible(true)
    // on its first activation) and by go.SetActive(true) don't re-fire our OnEnable and
    // collapse the paper back to rolled state.
    private void HandleUnrollComplete()
    {
        suppressOnEnable = true;
        try
        {
            if (leaderboard != null) leaderboard.enabled = true;
            if (go != null) go.SetActive(true);
        }
        finally
        {
            suppressOnEnable = false;
        }
        onUnrollComplete?.Invoke();
    }

    private void SetUnrollProgress(float progress01)
    {
        Vector3 s = originalLocalScale;
        switch (direction)
        {
            case UnrollDirection.TopToBottom:
            case UnrollDirection.BottomToTop:
                s.y = originalLocalScale.y * progress01;
                break;
            case UnrollDirection.LeftToRight:
            case UnrollDirection.RightToLeft:
                s.x = originalLocalScale.x * progress01;
                break;
        }
        rt.localScale = s;
    }

    private void ApplyPivotForDirection()
    {
        Vector2 desired;
        switch (direction)
        {
            case UnrollDirection.TopToBottom: desired = new Vector2(originalPivot.x, 1f); break;
            case UnrollDirection.BottomToTop: desired = new Vector2(originalPivot.x, 0f); break;
            case UnrollDirection.LeftToRight: desired = new Vector2(0f, originalPivot.y); break;
            case UnrollDirection.RightToLeft: desired = new Vector2(1f, originalPivot.y); break;
            default: desired = originalPivot; break;
        }
        SetPivotPreservePosition(rt, desired);
    }

    // Moves the pivot without visually shifting the rect's bounding box on screen. The rect's
    // bottom-left in parent space is `localPosition - pivot * size * localScale`, so when pivot
    // changes from p_old to p_new we must add `(p_new - p_old) * size * localScale` to
    // localPosition to keep the bottom-left (and therefore the whole bounding box) fixed.
    private static void SetPivotPreservePosition(RectTransform rect, Vector2 pivot)
    {
        Vector2 size = rect.rect.size;
        Vector2 deltaPivot = pivot - rect.pivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y, 0f);
        deltaPosition = Vector3.Scale(deltaPosition, rect.localScale);
        rect.pivot = pivot;
        rect.localPosition += deltaPosition;
    }

    // EaseOutCubic when backAmount == 0, EaseOutBack with tunable overshoot otherwise.
    private static float EaseOutBack(float t, float backAmount)
    {
        if (backAmount <= 0.0001f)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }
        float c1 = backAmount * 4f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
