using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OutcomeCardDissolve : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The card Image inside this GameObject. Auto-uses this GameObject's Image if left empty. " +
             "The Question_card final color will match this card's current Image color, so set them consistently.")]
    public Image sourceCard;

    [Header("Timing")]
    [Tooltip("Seconds to wait after the page activates before the fly begins. Match this to yellow_card/Red_card's slide-in duration.")]
    public float delayBeforeFly = 1.05f;
    [Tooltip("Time, in seconds, for Phase A — the card flies from its rest spot to the target Question_card's position. Size and rotation stay at their starting values during this phase.")]
    public float flyDuration = 0.5775f;
    [Tooltip("Time, in seconds, for Phase B — once at the target's position, the card rotates in place to match the target's rotation. Position and size do not change during this phase.")]
    public float rotateMatchDuration = 0.30f;
    [Tooltip("Time, in seconds, for Phase C — after rotating, the card scales in place to match the target's size. Position and rotation do not change during this phase.")]
    public float scaleMatchDuration = 0.30f;

    [Header("Misc")]
    public bool useUnscaledTime = true;

    // Pending schedule, set by OutcomeCardDissolveScheduler.ScheduleOrApply before the page is enabled.
    [System.NonSerialized] public int pendingSlot = -1;
    [System.NonSerialized] public bool hasPending;

    public RectTransform sourceRT;
    public Transform restParent;
    private int restSiblingIndex;
    private Vector2 restAnchoredPos;
    private Vector3 restLocalScale;
    private Quaternion restLocalRot;
    private Color restColor;
    private bool restCaptured;
    private Coroutine activeCo;

    private void Awake()
    {
        if (sourceCard == null) sourceCard = GetComponent<Image>();
        if (sourceCard != null)
        {
            sourceRT = sourceCard.rectTransform;
            CaptureRest();
        }
    }

    private void OnEnable()
    {
        if (sourceCard == null) return;
        ResetToRest();

        if (hasPending)
        {
            int slot = pendingSlot;
            hasPending = false;
            pendingSlot = -1;
            if (slot >= 0)
            {
                if (activeCo != null) StopCoroutine(activeCo);
                activeCo = StartCoroutine(PlayCo(slot));
            }
        }
    }

    private void OnDisable()
    {
        if (activeCo != null) { StopCoroutine(activeCo); activeCo = null; }
    }

    public void Schedule(int slot)
    {
        pendingSlot = slot;
        hasPending = true;
    }

    private void CaptureRest()
    {
        if (sourceRT == null) return;
        restParent = sourceRT.parent;
        restSiblingIndex = sourceRT.GetSiblingIndex();
        restAnchoredPos = sourceRT.anchoredPosition;
        restLocalScale = sourceRT.localScale;
        restLocalRot = sourceRT.localRotation;
        restColor = sourceCard.color;
        restCaptured = true;
    }

    private void ResetToRest()
    {
        if (sourceRT == null || !restCaptured) return;
        if (sourceRT.parent != restParent) sourceRT.SetParent(restParent, false);
        sourceRT.SetSiblingIndex(restSiblingIndex);
        sourceRT.anchoredPosition = restAnchoredPos;
        sourceRT.localScale = restLocalScale;
        sourceRT.localRotation = restLocalRot;
        sourceCard.color = restColor;
        sourceCard.gameObject.SetActive(true);
    }

    private IEnumerator PlayCo(int slotIdx)
    {
        // Defensive: re-apply painted slots before anything else — covers the case where
        // BgSlideIn / IncorrectPageAnimator activation cascade silently whitened a slot.
        if (QnaCardTracker.instance != null) QnaCardTracker.instance.RepaintAllPaintedSlots();

        Image target = FindTargetCard(slotIdx);
        if (target == null)
        {
            // No target to fly to — just sync the colour across all copies and bail.
            if (QnaCardTracker.instance != null) QnaCardTracker.instance.Mark(slotIdx, sourceCard.color);
            activeCo = null;
            yield break;
        }

        // Phase A: wait for yellow_card/Red_card slide-in to finish.
        if (delayBeforeFly > 0f) yield return WaitCo(delayBeforeFly);

        // Final color the Question_card will adopt — read NOW so callers can change
        // sourceCard.color at runtime before the fly starts.
        Color finalColor = sourceCard.color;

        // Reparent to the canvas root so the fly position + scale are independent of the
        // yellow_card / Red_card's own transform.
        Vector3 flyStartWorld = sourceRT.position;
        Canvas canvas = sourceCard.canvas;
        Transform stableParent = canvas != null ? canvas.transform : sourceRT.root;
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(target.rectTransform);
        sourceRT.SetParent(stableParent, true);
        sourceRT.SetAsLastSibling();
        sourceRT.position = flyStartWorld;

        // ForceRebuildLayoutImmediate above can subtly disturb the target's serialized
        // colour state in some Unity versions; re-apply painted slots NOW so an earlier-
        // question Question_card (e.g. Q1's green) isn't left at the inspector default.
        if (QnaCardTracker.instance != null) QnaCardTracker.instance.RepaintAllPaintedSlots();

        // Compute the localScale needed so the source's on-screen size EXACTLY matches
        // the target Question_card's on-screen size in BOTH axes. The previous version
        // used a single shared multiplier from the X-axis ratio, which left the Y axis
        // off whenever the source's aspect ratio differed from the target's. Per-axis
        // ratios fix this: source ends up at the target's exact width and height.
        RectTransform targetRT = target.rectTransform;
        Vector2 sourceWorldSize = Vector2.Scale(sourceRT.rect.size, sourceRT.lossyScale);
        Vector2 targetWorldSize = Vector2.Scale(targetRT.rect.size, targetRT.lossyScale);
        float scaleFactorX = sourceWorldSize.x > 0.0001f ? targetWorldSize.x / sourceWorldSize.x : 1f;
        float scaleFactorY = sourceWorldSize.y > 0.0001f ? targetWorldSize.y / sourceWorldSize.y : 1f;
        Vector3 endLocalScale = new Vector3(
            sourceRT.localScale.x * scaleFactorX,
            sourceRT.localScale.y * scaleFactorY,
            sourceRT.localScale.z);
        // World rotation, not localRotation: the source was reparented to the canvas root
        // a few lines above, while the target lives elsewhere in the hierarchy, so their
        // local rotations are not directly comparable — world rotation is.
        Quaternion endWorldRotation = targetRT.rotation;

        // ---------- PHASE A — FLY ONLY ----------
        // Source moves to the target's position. Size and rotation stay at their starting
        // values throughout this phase, so visually the original card just glides into
        // place looking exactly as it did at rest.
        Vector3 flyStartPos = sourceRT.position;
        float t = 0f;
        float dur = Mathf.Max(0.001f, flyDuration);
        while (t < dur)
        {
            t += DT();
            float u = Mathf.Clamp01(t / dur);
            float eased = EaseInOutCubic(u);
            sourceRT.position = Vector3.LerpUnclamped(flyStartPos, target.transform.position, eased);
            yield return null;
        }
        sourceRT.position = target.transform.position;

        // ---------- PHASE B — ROTATE IN PLACE ----------
        // Rotate to match the target's world rotation. Position and size do not change,
        // so the card visibly twists into the target's orientation while staying anchored
        // to the target's position.
        Quaternion rotateStart = sourceRT.rotation;
        if (rotateMatchDuration > 0f && Quaternion.Angle(rotateStart, endWorldRotation) > 0.01f)
        {
            t = 0f;
            dur = Mathf.Max(0.001f, rotateMatchDuration);
            while (t < dur)
            {
                t += DT();
                float u = Mathf.Clamp01(t / dur);
                float eased = EaseInOutCubic(u);
                sourceRT.rotation = Quaternion.SlerpUnclamped(rotateStart, endWorldRotation, eased);
                // Re-pin position each frame in case any parent transform shifted between
                // phases — the source must STAY at the target's position throughout.
                sourceRT.position = target.transform.position;
                yield return null;
            }
        }
        sourceRT.rotation = endWorldRotation;
        sourceRT.position = target.transform.position;

        // ---------- PHASE C — SCALE IN PLACE ----------
        // Now that position and rotation already match, grow (or shrink) the source's
        // scale until its world size equals the target's. Position and rotation are kept
        // locked to the target each frame.
        Vector3 scaleStart = sourceRT.localScale;
        if (scaleMatchDuration > 0f)
        {
            t = 0f;
            dur = Mathf.Max(0.001f, scaleMatchDuration);
            while (t < dur)
            {
                t += DT();
                float u = Mathf.Clamp01(t / dur);
                float eased = EaseInOutCubic(u);
                sourceRT.localScale = Vector3.LerpUnclamped(scaleStart, endLocalScale, eased);
                sourceRT.position = target.transform.position;
                sourceRT.rotation = endWorldRotation;
                yield return null;
            }
        }
        sourceRT.localScale = endLocalScale;
        sourceRT.position = target.transform.position;
        sourceRT.rotation = endWorldRotation;

        // Snap, not cross-fade. Target adopts the source's color in a single frame, and
        // the source is deactivated. No blending of two overlapping cards.
        target.color = finalColor;
        // Sync the OTHER bg copies via the tracker so subsequent pages show the same color.
        if (QnaCardTracker.instance != null) QnaCardTracker.instance.Mark(slotIdx, finalColor);

        // Restore the source's rest transform + color BEFORE deactivating so that when
        // OutcomeCardDissolveScheduler reactivates the GameObject next time, the OnEnable's
        // ResetToRest already finds it in the right state. Doing the parent reset first
        // also avoids leaving the source parented under the canvas root while inactive.
        if (sourceRT.parent != restParent) sourceRT.SetParent(restParent, false);
        sourceRT.SetSiblingIndex(restSiblingIndex);
        sourceRT.anchoredPosition = restAnchoredPos;
        sourceRT.localScale = restLocalScale;
        sourceRT.localRotation = restLocalRot;
        sourceCard.color = restColor;

        activeCo = null;
        // This line must be last: SetActive(false) on our own GameObject will stop the
        // coroutine immediately, so any work that comes after is skipped.
        sourceCard.gameObject.SetActive(false);
    }

    // Walk up from this 'card' GameObject to find its page (e.g. Incorrect_page), then
    // descend through Question_bg* > Question_card_holder > the slotIdx-th Image child
    // by SIBLING ORDER (not by name). Sibling order matches visual order in a Layout-
    // Group, so slot 0 always targets the leftmost visible card.
    private Image FindTargetCard(int slotIdx)
    {
        // 'this' is the card. Parent is yellow_card / Red_card. Grandparent is the page.
        Transform parentOfHolder = transform.parent;
        if (parentOfHolder == null) return null;
        Transform pageRoot = parentOfHolder.parent;
        if (pageRoot == null) return null;

        Transform questionBg = null;
        for (int i = 0; i < pageRoot.childCount; i++)
        {
            Transform c = pageRoot.GetChild(i);
            if (c.name.StartsWith("Question_bg")) { questionBg = c; break; }
        }
        if (questionBg == null) return null;

        Transform holder = FindChildDeep(questionBg, "Question_card_holder");
        if (holder == null) return null;

        return QnaCardTracker.GetCardBySiblingIndex(holder, slotIdx);
    }

    private static Transform FindChildDeep(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name) return c;
            Transform deeper = FindChildDeep(c, name);
            if (deeper != null) return deeper;
        }
        return null;
    }

    private IEnumerator WaitCo(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += DT();
            yield return null;
        }
    }

    private float DT() { return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; }

    private static float EaseInOutCubic(float t)
    {
        if (t < 0.5f) return 4f * t * t * t;
        float f = 2f * t - 2f;
        return 0.5f * f * f * f + 1f;
    }
}

public static class OutcomeCardDissolveScheduler
{
    // Called by game_flow_loop right before SetActive(page, true). If a dissolve component
    // is found under the target page, queues the slot for its OnEnable to consume. If not,
    // falls back to an immediate Mark so the Question_card still gets the right color.
    //
    // IMPORTANT: the dissolve coroutine ends with SetActive(false) on the source card's
    // GameObject, which means activeSelf stays false even when the page reactivates. We
    // reactivate it here so the next page activation triggers its OnEnable and the new
    // animation runs.
    public static void ScheduleOrApply(GameObject targetPage, int slotIdx, Color fallbackColor)
    {
        if (targetPage == null) return;

        OutcomeCardDissolve dissolve = targetPage.GetComponentInChildren<OutcomeCardDissolve>(true);
        if (dissolve != null)
        {
            dissolve.Schedule(slotIdx);
            if (!dissolve.gameObject.activeSelf) dissolve.gameObject.SetActive(true);
        }
        else if (QnaCardTracker.instance != null)
        {
            QnaCardTracker.instance.Mark(slotIdx, fallbackColor);
        }
    }
}
