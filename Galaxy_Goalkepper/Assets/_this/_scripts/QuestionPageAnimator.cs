using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class QuestionPageAnimator : MonoBehaviour
{
    [Header("References (auto-filled if left empty)")]
    public RectTransform question;
    public CanvasGroup questionTextCg;
    public RectTransform[] options;
    public CanvasGroup[] optionCgs;
    public Image[] optionImages;
    public RectTransform[] questionCards;
    public CanvasGroup[] questionCardCgs;
    public Image[] questionCardImages;
    [Tooltip("Parent of Question_card_holder. Slides in from the left before the cards animate.")]
    public RectTransform questionBg;
    public Question_manager questionManager;
    public DraggableBall answerBall;
    public CanvasGroup pageCanvasGroup;

    [Header("Step 1 — question popup + rotation")]
    public float questionPopDuration = 1.235f;
    [Tooltip("Whole number so the rotation lands at 0deg smoothly.")]
    public float questionRotations = 1f;
    public float questionPopOvershoot = 1.0f;
    public float questionToTextGap = 0.19f;

    [Header("Step 2 — question_Text fade-in")]
    public float questionTextFadeDuration = 0.8075f;
    public float textToOptionsGap = 0.285f;

    [Header("Step 3 — options drop + fill + fade (one by one)")]
    [Tooltip("Pixels each option starts ABOVE its resting position before dropping down.")]
    public float optionsDropDistance = 40f;
    [Tooltip("Per-option animation duration.")]
    public float optionsDropDuration = 0.8075f;
    [Tooltip("Delay between each option starting (stagger).")]
    public float optionsStaggerGap = 0.19f;
    public float optionsToCardsGap = 0.285f;

    [Header("Step 4 — Question_bg slide-in (before cards)")]
    [Tooltip("If true, drives the BgSlideIn helper on Question_bg at this step. Slide-in parameters (offset, duration, ease) live on the BgSlideIn component so they're shared across every Question_bg in the scene.")]
    public bool slideInQuestionBg = true;
    [Tooltip("Pause after Question_bg lands, before Step 5 (cards) starts.")]
    public float questionBgToCardsGap = 0.1425f;

    [Header("Step 5 — question cards drop + fill + fade (one by one)")]
    public float cardsDropDistance = 40f;
    public float cardsDropDuration = 0.76f;
    public float cardsStaggerGap = 0.1425f;

    [Header("Answer_ball roll-in (parallel with Step 3 — options)")]
    [Tooltip("If true, the ball rolls in from the left at the moment options begin to appear.")]
    public bool rollInBall = true;
    [Tooltip("How far to the LEFT of its rest position the ball starts. Use a negative value (e.g. -1200) to begin off-screen-left for a 1080-wide page.")]
    public float ballRollStartOffsetX = -1200f;
    [Tooltip("Total time, in seconds, for the ball to travel from its start offset to its rest position. Rotation speed is derived from this so the ball never slips.")]
    public float ballRollDuration = 1.235f;
    [Tooltip("Override the rolling radius (pixels). Leave 0 to use half the ball's RectTransform width.")]
    public float ballRollRadiusOverride = 0f;
    [Tooltip("If true, the ball rotates clockwise while moving right (physically correct for a ball rolling on a floor below it).")]
    public bool ballRollClockwiseRight = true;
    [Tooltip("Ease the linear motion. If false, motion is perfectly constant (true linear speed = constant rotation speed).")]
    public bool ballRollEaseOut = false;

    [Header("Misc")]
    public bool useUnscaledTime = true;

    private Vector3 questionRestScale = Vector3.one;
    private Quaternion questionRestRotation = Quaternion.identity;
    private float questionTextRestAlpha = 1f;
    private Vector2[] optionRestPositions;
    private float[] optionRestAlphas;
    private float[] optionRestFill;
    private Vector2[] cardRestPositions;
    private float[] cardRestAlphas;
    private float[] cardRestFill;
    private Vector2 ballRestPosition;
    private Quaternion ballRestRotation = Quaternion.identity;
    private bool ballRestCaptured;
    private BgSlideIn questionBgSlideIn;
    private bool captured;

    private void Awake()
    {
        AutoFind();
        Capture();
    }

    private void OnEnable()
    {
        if (!captured)
        {
            AutoFind();
            Capture();
        }
        // Heal any Question_card colour that was silently reset since the last Mark.
        if (QnaCardTracker.instance != null) QnaCardTracker.instance.RepaintAllPaintedSlots();
        StopAllCoroutines();
        StartCoroutine(Play());
    }

    private void AutoFind()
    {
        if (questionManager == null) questionManager = FindObjectOfType<Question_manager>();

        if (pageCanvasGroup == null)
        {
            pageCanvasGroup = GetComponent<CanvasGroup>();
            if (pageCanvasGroup == null) pageCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (question == null)
        {
            Transform t = FindChildByName(transform, "question");
            if (t == null) t = FindChildByName(transform, "Question");
            if (t == null) t = FindChildByName(transform, "question_holder");
            if (t == null) t = FindChildByName(transform, "question_box");
            if (t == null) t = FindChildByName(transform, "question_bg");
            if (t == null) t = FindChildByName(transform, "Question_holder");
            if (t != null) question = t as RectTransform;
        }

        if (questionTextCg == null)
        {
            Transform t = FindChildByName(transform, "question_Text (TMP)");
            if (t == null) t = FindChildByName(transform, "Question_Text (TMP)");
            if (t == null) t = FindChildByNameContainsCI(transform, "question_text");
            if (t == null && questionManager != null && questionManager.question_txt != null) t = questionManager.question_txt.transform;
            if (t != null)
            {
                questionTextCg = t.GetComponent<CanvasGroup>();
                if (questionTextCg == null) questionTextCg = t.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (options == null || options.Length == 0)
        {
            List<RectTransform> opts = new List<RectTransform>();
            if (questionManager != null && questionManager.answerButtons != null)
            {
                for (int i = 0; i < questionManager.answerButtons.Length; i++)
                {
                    Button b = questionManager.answerButtons[i];
                    if (b != null) opts.Add(b.transform as RectTransform);
                }
            }
            if (opts.Count == 0)
            {
                for (int i = 1; i <= 4; i++)
                {
                    Transform t = FindChildByName(transform, "option_" + i);
                    if (t == null) t = FindChildByName(transform, "Option_" + i);
                    if (t == null) t = FindChildByName(transform, "option" + i);
                    if (t != null) opts.Add(t as RectTransform);
                }
            }
            options = opts.ToArray();
        }
        EnsureCanvasGroups(options, ref optionCgs);
        EnsureImages(options, ref optionImages);

        if (questionCards == null || questionCards.Length == 0)
        {
            List<RectTransform> cards = new List<RectTransform>();
            if (QnaCardTracker.instance != null && QnaCardTracker.instance.cards != null)
            {
                for (int i = 0; i < QnaCardTracker.instance.cards.Length; i++)
                {
                    Image img = QnaCardTracker.instance.cards[i];
                    if (img != null) cards.Add(img.transform as RectTransform);
                }
            }
            if (cards.Count == 0)
            {
                string[] names = { "Question_card", "Question_card (1)", "Question_card (2)", "Question_card (3)", "Question_card (4)" };
                for (int i = 0; i < names.Length; i++)
                {
                    Transform t = FindChildByName(transform, names[i]);
                    if (t == null) t = FindSceneObjectByName(names[i]);
                    if (t != null) cards.Add(t as RectTransform);
                }
            }
            questionCards = cards.ToArray();
        }
        EnsureCanvasGroups(questionCards, ref questionCardCgs);
        EnsureImages(questionCards, ref questionCardImages);

        if (questionBg == null)
        {
            Transform t = FindChildByName(transform, "Question_bg");
            if (t == null) t = FindChildByName(transform, "question_bg");
            if (t == null) t = FindChildByNameContainsCI(transform, "question_bg");
            if (t != null) questionBg = t as RectTransform;
        }

        if (questionBg != null && questionBgSlideIn == null)
        {
            questionBgSlideIn = questionBg.GetComponent<BgSlideIn>();
        }
        if (questionBgSlideIn != null)
        {
            // We schedule this bg's slide ourselves at Step 4; don't let the helper auto-fire on enable.
            questionBgSlideIn.autoPlayOnEnable = false;
        }

        if (answerBall == null)
        {
            if (questionManager != null) answerBall = questionManager.answerBall;
            if (answerBall == null)
            {
                GameObject go = GameObject.Find("Answer_ball");
                if (go != null) answerBall = go.GetComponent<DraggableBall>();
            }
        }
    }

    private void EnsureCanvasGroups(RectTransform[] rts, ref CanvasGroup[] cgs)
    {
        if (rts == null) { cgs = new CanvasGroup[0]; return; }
        if (cgs == null || cgs.Length != rts.Length) cgs = new CanvasGroup[rts.Length];
        for (int i = 0; i < rts.Length; i++)
        {
            if (rts[i] == null) continue;
            if (cgs[i] != null) continue;
            CanvasGroup cg = rts[i].GetComponent<CanvasGroup>();
            if (cg == null) cg = rts[i].gameObject.AddComponent<CanvasGroup>();
            cgs[i] = cg;
        }
    }

    private void EnsureImages(RectTransform[] rts, ref Image[] imgs)
    {
        if (rts == null) { imgs = new Image[0]; return; }
        if (imgs == null || imgs.Length != rts.Length) imgs = new Image[rts.Length];
        for (int i = 0; i < rts.Length; i++)
        {
            if (rts[i] == null) continue;
            if (imgs[i] != null) continue;
            imgs[i] = rts[i].GetComponent<Image>();
        }
    }

    private void Capture()
    {
        if (question != null)
        {
            if (question.localScale.sqrMagnitude > 0.0001f) questionRestScale = question.localScale;
            questionRestRotation = question.localRotation;
        }
        if (questionTextCg != null && questionTextCg.alpha > 0f) questionTextRestAlpha = questionTextCg.alpha;

        if (options != null)
        {
            optionRestPositions = new Vector2[options.Length];
            optionRestAlphas = new float[options.Length];
            optionRestFill = new float[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null) optionRestPositions[i] = options[i].anchoredPosition;
                float a = 1f;
                if (optionCgs != null && i < optionCgs.Length && optionCgs[i] != null && optionCgs[i].alpha > 0f) a = optionCgs[i].alpha;
                optionRestAlphas[i] = a;
                float f = 1f;
                if (optionImages != null && i < optionImages.Length && optionImages[i] != null && optionImages[i].type == Image.Type.Filled)
                    f = optionImages[i].fillAmount > 0f ? optionImages[i].fillAmount : 1f;
                optionRestFill[i] = f;
            }
        }

        if (questionCards != null)
        {
            cardRestPositions = new Vector2[questionCards.Length];
            cardRestAlphas = new float[questionCards.Length];
            cardRestFill = new float[questionCards.Length];
            for (int i = 0; i < questionCards.Length; i++)
            {
                if (questionCards[i] != null) cardRestPositions[i] = questionCards[i].anchoredPosition;
                float a = 1f;
                if (questionCardCgs != null && i < questionCardCgs.Length && questionCardCgs[i] != null && questionCardCgs[i].alpha > 0f) a = questionCardCgs[i].alpha;
                cardRestAlphas[i] = a;
                float f = 1f;
                if (questionCardImages != null && i < questionCardImages.Length && questionCardImages[i] != null && questionCardImages[i].type == Image.Type.Filled)
                    f = questionCardImages[i].fillAmount > 0f ? questionCardImages[i].fillAmount : 1f;
                cardRestFill[i] = f;
            }
        }

        if (answerBall != null && !ballRestCaptured)
        {
            RectTransform ballRT = answerBall.transform as RectTransform;
            if (ballRT != null)
            {
                ballRestPosition = ballRT.anchoredPosition;
                ballRestRotation = ballRT.localRotation;
                ballRestCaptured = true;
                answerBall.SetHomeAnchoredPosition(ballRestPosition);
            }
        }

        captured = true;
    }

    private IEnumerator Play()
    {
        BlockInput(true);

        if (question != null) { question.localScale = Vector3.zero; question.localRotation = questionRestRotation; }
        if (questionTextCg != null) questionTextCg.alpha = 0f;

        HideAll(options, optionCgs, optionImages, optionRestPositions, optionsDropDistance);

        // Park Question_bg off-screen-left + hide its cards via the helper. The helper
        // staggers the cards in as part of its own Play sequence, so QuestionPageAnimator
        // no longer needs a separate cards step.
        if (slideInQuestionBg && questionBgSlideIn != null)
        {
            questionBgSlideIn.ParkOffscreen();
        }

        // Hide the Answer_ball until its roll-in starts (otherwise it briefly flashes at rest
        // while the question pops + text fades). RollBallIn restores alpha = 1 once the ball
        // has been parked off-screen-left, so the roll begins from invisible-to-visible-rolling.
        if (rollInBall && answerBall != null)
        {
            CanvasGroup ballCG = answerBall.GetComponent<CanvasGroup>();
            if (ballCG != null) ballCG.alpha = 0f;
        }

        // Step 1: question pops with rotation
        if (question != null) yield return RotatePopup(question);
        if (questionToTextGap > 0f) yield return Wait(questionToTextGap);

        // Step 2: question_Text fades in
        if (questionTextCg != null) yield return FadeInCG(questionTextCg, questionTextRestAlpha, questionTextFadeDuration);
        if (textToOptionsGap > 0f) yield return Wait(textToOptionsGap);

        // Ball rolls in from the left in parallel with Step 3.
        if (rollInBall && answerBall != null && ballRestCaptured)
        {
            StartCoroutine(RollBallIn());
        }

        // Steps 3 + 4 run IN PARALLEL: options drop in at the same time Question_bg
        // slides in and stages its child cards. Kicked off as separate coroutines so
        // the two timelines overlap; the rest of Play() waits for both to finish before
        // unblocking input.
        Coroutine optionsCo = null;
        if (options != null && options.Length > 0)
            optionsCo = StartCoroutine(AppearStaggered(options, optionCgs, optionImages, optionRestPositions, optionRestAlphas, optionRestFill, optionsDropDistance, optionsDropDuration, optionsStaggerGap));

        Coroutine bgCo = null;
        if (slideInQuestionBg && questionBgSlideIn != null)
            bgCo = StartCoroutine(questionBgSlideIn.PlayCoroutine());

        if (optionsCo != null) yield return optionsCo;
        if (bgCo != null) yield return bgCo;

        // After the cards have finished their alpha 0→1 slide-in, re-apply every painted slot
        // so any earlier-answered Question_card shows its outcome colour on this next question
        // — covers the "Q1 painted green, Q2 shows the card white" bug where some mechanism
        // between the previous Mark and the cards becoming visible silently re-whitened them.
        if (QnaCardTracker.instance != null) QnaCardTracker.instance.RepaintAllPaintedSlots();

        // Unlock answer click + drag input only AFTER the Question_bg + cards have fully
        // slid in. If we unblock earlier, an eager tap interrupts this Play() coroutine —
        // the question_page is SetActive(false) by CorrectPageAnimator mid-animation, the
        // final RepaintAllPaintedSlots above never fires, and the Question_card colours
        // end up out of sync on the next question.
        BlockInput(false);
    }

    private void HideAll(RectTransform[] rts, CanvasGroup[] cgs, Image[] imgs, Vector2[] restPos, float dropDistance)
    {
        if (rts == null) return;
        for (int i = 0; i < rts.Length; i++)
        {
            if (rts[i] != null) rts[i].anchoredPosition = restPos[i] + new Vector2(0f, dropDistance);
            if (cgs != null && i < cgs.Length && cgs[i] != null) cgs[i].alpha = 0f;
            if (imgs != null && i < imgs.Length && imgs[i] != null && imgs[i].type == Image.Type.Filled) imgs[i].fillAmount = 0f;
        }
    }

    private void BlockInput(bool block)
    {
        if (pageCanvasGroup != null) pageCanvasGroup.blocksRaycasts = !block;
        // Intentionally do NOT toggle answerBall.enabled — the user wants the ball
        // to keep spinning continuously. Drag input is gated by pageCanvasGroup.blocksRaycasts.
    }

    private IEnumerator RotatePopup(RectTransform rt)
    {
        rt.localScale = Vector3.zero;
        rt.localRotation = questionRestRotation;
        float dur = Mathf.Max(0.001f, questionPopDuration);
        float t = 0f;
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            rt.localScale = questionRestScale * EaseOutBack(u, questionPopOvershoot);
            float rotU = EaseOutCubic(u);
            float rotZ = -360f * questionRotations * rotU;
            rt.localRotation = questionRestRotation * Quaternion.Euler(0f, 0f, rotZ);
            yield return null;
        }
        rt.localScale = questionRestScale;
        rt.localRotation = questionRestRotation;
    }

    private IEnumerator AppearStaggered(RectTransform[] rts, CanvasGroup[] cgs, Image[] imgs, Vector2[] restPos, float[] restAlpha, float[] restFill, float dropDistance, float perItemDuration, float staggerGap)
    {
        if (rts == null || rts.Length == 0) yield break;
        Coroutine[] running = new Coroutine[rts.Length];
        for (int i = 0; i < rts.Length; i++)
        {
            float delay = i * Mathf.Max(0f, staggerGap);
            running[i] = StartCoroutine(AppearWithDelay(i, delay, rts, cgs, imgs, restPos, restAlpha, restFill, dropDistance, perItemDuration));
        }
        for (int i = 0; i < running.Length; i++)
        {
            if (running[i] != null) yield return running[i];
        }
    }

    private IEnumerator AppearWithDelay(int idx, float delay, RectTransform[] rts, CanvasGroup[] cgs, Image[] imgs, Vector2[] restPos, float[] restAlpha, float[] restFill, float dropDistance, float duration)
    {
        if (delay > 0f) yield return Wait(delay);
        yield return AppearOne(idx, rts, cgs, imgs, restPos, restAlpha, restFill, dropDistance, duration);
    }

    private IEnumerator AppearOne(int idx, RectTransform[] rts, CanvasGroup[] cgs, Image[] imgs, Vector2[] restPos, float[] restAlpha, float[] restFill, float dropDistance, float duration)
    {
        if (idx < 0 || idx >= rts.Length || rts[idx] == null) yield break;

        Vector2 startPos = restPos[idx] + new Vector2(0f, dropDistance);
        rts[idx].anchoredPosition = startPos;
        if (cgs != null && idx < cgs.Length && cgs[idx] != null) cgs[idx].alpha = 0f;
        if (imgs != null && idx < imgs.Length && imgs[idx] != null && imgs[idx].type == Image.Type.Filled) imgs[idx].fillAmount = 0f;

        float dur = Mathf.Max(0.001f, duration);
        float t = 0f;
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float eased = EaseOutCubic(u);
            rts[idx].anchoredPosition = Vector2.LerpUnclamped(startPos, restPos[idx], eased);
            if (cgs != null && idx < cgs.Length && cgs[idx] != null && restAlpha != null && idx < restAlpha.Length)
                cgs[idx].alpha = u * restAlpha[idx];
            if (imgs != null && idx < imgs.Length && imgs[idx] != null && imgs[idx].type == Image.Type.Filled && restFill != null && idx < restFill.Length)
                imgs[idx].fillAmount = eased * restFill[idx];
            yield return null;
        }

        rts[idx].anchoredPosition = restPos[idx];
        if (cgs != null && idx < cgs.Length && cgs[idx] != null && restAlpha != null && idx < restAlpha.Length)
            cgs[idx].alpha = restAlpha[idx];
        if (imgs != null && idx < imgs.Length && imgs[idx] != null && imgs[idx].type == Image.Type.Filled && restFill != null && idx < restFill.Length)
            imgs[idx].fillAmount = restFill[idx];
    }

    private IEnumerator RollBallIn()
    {
        if (answerBall == null) yield break;
        RectTransform ballRT = answerBall.transform as RectTransform;
        if (ballRT == null) yield break;

        // Pause the constant Update spin only while the ball is rolling, so the rolling
        // rotation (locked to translation) isn't fighting an additive spin and skidding.
        float savedRotationSpeed = answerBall.rotationSpeed;
        answerBall.rotationSpeed = 0f;

        Vector2 startPos = ballRestPosition + new Vector2(ballRollStartOffsetX, 0f);
        ballRT.anchoredPosition = startPos;
        ballRT.localRotation = ballRestRotation;

        // Now that the ball is off-screen-left, make it visible again. Play() hid it via
        // alpha = 0 so it wouldn't flash at rest during the question pop / text fade.
        CanvasGroup ballCG = answerBall.GetComponent<CanvasGroup>();
        if (ballCG != null) ballCG.alpha = 1f;

        float radius = ballRollRadiusOverride > 0f ? ballRollRadiusOverride : ballRT.rect.width * 0.5f;
        if (radius <= 0.0001f) radius = 50f;
        float circumference = 2f * Mathf.PI * radius;

        float dur = Mathf.Max(0.001f, ballRollDuration);
        float t = 0f;
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float eased = ballRollEaseOut ? EaseOutCubic(u) : u;
            Vector2 newPos = Vector2.LerpUnclamped(startPos, ballRestPosition, eased);
            ballRT.anchoredPosition = newPos;

            float dx = newPos.x - startPos.x;
            float rotDeg = (dx / circumference) * 360f;
            if (ballRollClockwiseRight) rotDeg = -rotDeg;
            ballRT.localRotation = ballRestRotation * Quaternion.Euler(0f, 0f, rotDeg);

            yield return null;
        }

        ballRT.anchoredPosition = ballRestPosition;
        ballRT.localRotation = ballRestRotation;
        answerBall.SetHomeAnchoredPosition(ballRestPosition);

        // Restore continuous Update spin now that the roll has ended.
        answerBall.rotationSpeed = savedRotationSpeed;
    }

    private IEnumerator FadeInCG(CanvasGroup cg, float targetAlpha, float duration)
    {
        cg.alpha = 0f;
        float dur = Mathf.Max(0.001f, duration);
        float t = 0f;
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            cg.alpha = u * targetAlpha;
            yield return null;
        }
        cg.alpha = targetAlpha;
    }

    private IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private static float EaseOutBack(float t, float c1)
    {
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name) return c;
            Transform deeper = FindChildByName(c, name);
            if (deeper != null) return deeper;
        }
        return null;
    }

    private static Transform FindChildByNameContainsCI(Transform root, string needle)
    {
        if (root == null) return null;
        string lower = needle.ToLowerInvariant();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name != null && c.name.ToLowerInvariant().Contains(lower)) return c;
            Transform deeper = FindChildByNameContainsCI(c, needle);
            if (deeper != null) return deeper;
        }
        return null;
    }

    private static Transform FindSceneObjectByName(string name)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go.name != name) continue;
            if (!go.scene.IsValid()) continue;
            if (go.hideFlags != HideFlags.None) continue;
            return go.transform;
        }
        return null;
    }
}

public static class QuestionPageAnimatorAutoSetup
{
    private const string TargetName = "question_page";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        Run();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Run(); }

    private static void Run()
    {
        GameObject go = FindByName(TargetName);
        if (go == null)
        {
            Debug.LogWarning("[QuestionPageAnimatorAutoSetup] No GameObject named '" + TargetName + "' found in the scene.");
            return;
        }
        if (go.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning("[QuestionPageAnimatorAutoSetup] '" + TargetName + "' has no RectTransform; skipping.");
            return;
        }
        if (go.GetComponent<QuestionPageAnimator>() == null)
        {
            go.AddComponent<QuestionPageAnimator>();
        }
    }

    private static GameObject FindByName(string name)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go.name != name) continue;
            if (!go.scene.IsValid()) continue;
            if (go.hideFlags != HideFlags.None) continue;
            return go;
        }
        return null;
    }
}
