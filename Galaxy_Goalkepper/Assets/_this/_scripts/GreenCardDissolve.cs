using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Drives the correct_page's `greencard` Image: when CorrectPageAnimator reaches the gap
// between text_bg pop and correct_Text pop, the greencard flies + shrinks down to the
// matching Question_card slot under Question_bg, then cross-fades into the target. This
// mirrors OutcomeCardDissolve (yellow_card / Red_card) — same scheduler pattern, same
// FindTargetCard logic — but with an added scale lerp during the fly so the big green
// rectangle ends up at the small Question_card size.
[DisallowMultipleComponent]
public class GreenCardDissolve : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The greencard Image inside this GameObject. Auto-uses this GameObject's Image if left empty.")]
    public Image sourceCard;

    [Header("Timing")]
    [Tooltip("Time, in seconds, for the green card to fly + shrink from its rest position to the target Question_card.")]
    public float flyDuration = 0.6f;
    [Tooltip("Time, in seconds, for the cross-fade once the card has landed (source fades out as the Question_card color fades in).")]
    public float dissolveDuration = 0.4f;

    [Header("Misc")]
    public bool useUnscaledTime = true;

    // Set by GreenCardDissolveScheduler.ScheduleOrApply before correct_page is enabled.
    // CorrectPageAnimator.Play() reads them when it invokes PlayCoroutine().
    [System.NonSerialized] public int pendingSlot = -1;
    [System.NonSerialized] public Color pendingColor = Color.white;
    [System.NonSerialized] public bool hasPending;

    private RectTransform sourceRT;
    private Transform restParent;
    private int restSiblingIndex;
    private Vector2 restAnchoredPos;
    private Vector3 restLocalScale;
    private Quaternion restLocalRot;
    private Color restColor;
    private bool restCaptured;

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
        ResetToRest();
    }

    public void Schedule(int slot, Color finalColor)
    {
        pendingSlot = slot;
        pendingColor = finalColor;
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

    // Invoked by CorrectPageAnimator between the text_bg pop and the correct_Text pop.
    // If nothing was scheduled, marks the slot immediately and bails so the rest of the
    // correct page animation still completes correctly.
    public IEnumerator PlayCoroutine()
    {
        if (sourceCard == null) yield break;
        if (!hasPending) yield break;

        int slotIdx = pendingSlot;
        Color finalColor = pendingColor;
        hasPending = false;
        pendingSlot = -1;

        Image target = FindTargetCard(slotIdx);
        if (target == null)
        {
            if (QnaCardTracker.instance != null) QnaCardTracker.instance.Mark(slotIdx, finalColor);
            yield break;
        }

        Color sourceStartColor = sourceCard.color;
        Color targetStartColor = target.color;

        // Reparent to the canvas root so the fly position is independent of text_bg's
        // transform (and so the flying card draws on top of every other UI element).
        Vector3 flyStartWorld = sourceRT.position;
        Canvas canvas = sourceCard.canvas;
        Transform stableParent = canvas != null ? canvas.transform : sourceRT.root;
        sourceRT.SetParent(stableParent, true);
        sourceRT.SetAsLastSibling();

        Vector3 startLocalScale = sourceRT.localScale;
        // Match target's on-screen size: scale source so (rect.size * lossyScale) equals target's.
        // lossyScale captures every parent's contribution, so we don't need to walk hierarchies.
        RectTransform targetRT = target.rectTransform;
        Vector2 sourceWorldSize = Vector2.Scale(sourceRT.rect.size, sourceRT.lossyScale);
        Vector2 targetWorldSize = Vector2.Scale(targetRT.rect.size, targetRT.lossyScale);
        float scaleFactor = sourceWorldSize.x > 0.0001f ? targetWorldSize.x / sourceWorldSize.x : 1f;
        Vector3 endLocalScale = startLocalScale * scaleFactor;

        Vector3 endWorld = targetRT.position;

        // Phase A: fly + shrink toward the Question_card.
        float t = 0f;
        float dur = Mathf.Max(0.001f, flyDuration);
        while (t < dur)
        {
            t += DT();
            float u = Mathf.Clamp01(t / dur);
            float eased = EaseInOutCubic(u);
            sourceRT.position = Vector3.LerpUnclamped(flyStartWorld, endWorld, eased);
            sourceRT.localScale = Vector3.LerpUnclamped(startLocalScale, endLocalScale, eased);
            yield return null;
        }
        sourceRT.position = endWorld;
        sourceRT.localScale = endLocalScale;

        // Phase B: dissolve cross-fade — source alpha to 0, target color toward finalColor.
        t = 0f;
        dur = Mathf.Max(0.001f, dissolveDuration);
        while (t < dur)
        {
            t += DT();
            float u = Mathf.Clamp01(t / dur);
            float eased = EaseInOutCubic(u);

            Color sc = sourceStartColor;
            sc.a = sourceStartColor.a * (1f - eased);
            sourceCard.color = sc;

            target.color = Color.Lerp(targetStartColor, finalColor, eased);
            yield return null;
        }

        sourceCard.color = new Color(sourceStartColor.r, sourceStartColor.g, sourceStartColor.b, 0f);
        target.color = finalColor;

        // Sync the other Question_bg copies so subsequent pages show the same color.
        if (QnaCardTracker.instance != null) QnaCardTracker.instance.Mark(slotIdx, finalColor);

        // Park back at rest so the next OnEnable (next correct page activation) can fully reset us.
        if (sourceRT.parent != restParent) sourceRT.SetParent(restParent, false);
        sourceRT.SetSiblingIndex(restSiblingIndex);
        sourceRT.anchoredPosition = restAnchoredPos;
        sourceRT.localScale = restLocalScale;
        sourceRT.localRotation = restLocalRot;
    }

    // Walks up: greencard -> text_bg -> correct_page. Then descends into the page's
    // first Question_bg* child > Question_card_holder > the slotIdx-th Image child by
    // SIBLING ORDER (not by name). Sibling order matches visual order in a Horizontal/
    // VerticalLayoutGroup, so slot 0 always flies to the leftmost visible card under
    // this page — regardless of whatever the cards are named.
    private Image FindTargetCard(int slotIdx)
    {
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

    private float DT() { return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; }

    private static float EaseInOutCubic(float t)
    {
        if (t < 0.5f) return 4f * t * t * t;
        float f = 2f * t - 2f;
        return 0.5f * f * f * f + 1f;
    }
}

public static class GreenCardDissolveScheduler
{
    // Called by game_flow_loop right before SetActive(correctpage, true). If the
    // greencard already has the dissolve component, schedules the slot + finalColor for
    // its PlayCoroutine. Otherwise auto-attaches the component to the `greencard` child
    // under the page so the first correct answer still animates. Falls back to an
    // immediate Mark if no greencard child exists at all.
    public static void ScheduleOrApply(GameObject targetPage, int slotIdx, Color finalColor)
    {
        if (targetPage == null) return;

        GreenCardDissolve dissolve = targetPage.GetComponentInChildren<GreenCardDissolve>(true);
        if (dissolve == null)
        {
            Transform gc = FindChildDeep(targetPage.transform, "greencard");
            if (gc != null && gc.GetComponent<Image>() != null)
            {
                dissolve = gc.gameObject.AddComponent<GreenCardDissolve>();
            }
        }

        if (dissolve != null)
        {
            // The second-try-correct path in game_flow_loop SetActive(false)s the greencard
            // to skip its dissolve. Re-activate it here so a subsequent first-try correct
            // still gets the full animation.
            if (!dissolve.gameObject.activeSelf) dissolve.gameObject.SetActive(true);
            dissolve.Schedule(slotIdx, finalColor);
        }
        else if (QnaCardTracker.instance != null)
        {
            QnaCardTracker.instance.Mark(slotIdx, finalColor);
        }
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
}
