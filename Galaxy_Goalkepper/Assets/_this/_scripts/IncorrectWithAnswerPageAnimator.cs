using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class IncorrectWithAnswerPageAnimator : MonoBehaviour
{
    [Header("References (auto-filled if left empty)")]
    public RectTransform bg;
    [Tooltip("Image_Incorrect (1) — pops in between the bg pop and the Incorrect! text fade-in.")]
    public RectTransform incorrectImage;
    public CanvasGroup incorrectTextCg;
    public CanvasGroup answerTextCg;
    public GameObject redCard;
    public YellowCardSlideIn redCardSlide;
    public RectTransform continueButton;
    public Button continueButtonComp;
    public CanvasGroup continueButtonCg;
    public GameObject questionPage;

    [Header("Step 1 — bg pop")]
    public float bgPopDuration = 0.855f;
    public float bgToTextGap = 0.095f;
    [Tooltip("Bg uses its own (smaller) overshoot so the full-screen pop looks smooth.")]
    public float bgOvershoot = 0.4f;

    [Header("Step 1.5 — Image_Incorrect (1) pop")]
    [Tooltip("Pop duration (0 → resting scale) for Image_Incorrect (1), with overshoot from popOvershoot. Fires after the bg-to-text gap and before the Incorrect! text fade-in.")]
    public float incorrectImagePopDuration = 0.855f;
    [Tooltip("Pause between Image_Incorrect (1) popping in and the Incorrect! text fade-in starting.")]
    public float incorrectImageToTextGap = 0.095f;

    [Header("Step 2 — Incorrect! text fade-in")]
    public float incorrectTextFadeDelay = 0.0475f;
    public float incorrectTextFadeDuration = 0.76f;
    public float incorrectToAnswerGap = 0.19f;

    [Header("Step 3 — Answer text fade-in + Red_card slide (parallel — Red_card slide unchanged)")]
    public float answerTextFadeDuration = 0.76f;
    [Tooltip("Seconds to wait for Red_card slide. If <= 0, reads YellowCardSlideIn.duration automatically (with a 1s minimum).")]
    public float redCardWaitOverride = 0f;
    public float answerToContinueGap = 0.5225f;

    [Header("Step 4 — continue_button pop (very last)")]
    public float continueButtonPopDuration = 0.855f;
    public float popOvershoot = 1.0f;

    [Header("Misc")]
    public bool useUnscaledTime = true;

    private Vector3 bgRestScale = Vector3.one;
    private Vector3 incorrectImageRestScale = Vector3.one;
    private Vector3 continueButtonRestScale = Vector3.one;
    private float incorrectTextRestAlpha = 1f;
    private float answerTextRestAlpha = 1f;
    private float continueButtonRestAlpha = 1f;
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

        if (redCard != null && redCard.activeSelf) redCard.SetActive(false);

        if (QnaCardTracker.instance != null) QnaCardTracker.instance.RepaintAllPaintedSlots();

        StopAllCoroutines();
        StartCoroutine(Play());
    }

    private void AutoFind()
    {
        if (bg == null) bg = FindChildRT(transform, "bg");
        if (incorrectImage == null)
        {
            // This page uses the duplicate-named copy "Image_Incorrect (1)" — try that first,
            // then fall back to the un-suffixed name in case the duplicate is renamed later.
            incorrectImage = FindChildRT(transform, "Image_Incorrect (1)");
            if (incorrectImage == null) incorrectImage = FindChildRT(transform, "Image_Incorrect");
        }

        if (incorrectTextCg == null)
        {
            Transform t = FindChildByName(transform, "Incorrect!_Text (TMP)");
            if (t == null) t = FindChildByName(transform, "Incorrect_Text (TMP)");
            if (t == null) t = FindChildByNamePrefix(transform, "Incorrect");
            if (t != null)
            {
                incorrectTextCg = t.GetComponent<CanvasGroup>();
                if (incorrectTextCg == null) incorrectTextCg = t.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (answerTextCg == null)
        {
            Transform t = FindChildByName(transform, "answer_Text (TMP)");
            if (t == null) t = FindChildByName(transform, "Answer_Text (TMP)");
            if (t == null) t = FindChildByName(transform, "correct_answer_Text (TMP)");
            if (t == null) t = FindChildByName(transform, "Why_correct_Text (TMP)");
            if (t == null) t = FindChildByNameContainsCI(transform, "answer");
            if (t == null) t = FindChildByNamePrefixCI(transform, "Why_");
            if (t != null)
            {
                answerTextCg = t.GetComponent<CanvasGroup>();
                if (answerTextCg == null) answerTextCg = t.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (redCard == null)
        {
            Transform t = FindChildByName(transform, "Red_card");
            if (t == null) t = FindChildByName(transform, "red_card");
            if (t == null) t = FindChildByNamePrefixCI(transform, "red_card");
            if (t != null) redCard = t.gameObject;
        }
        if (redCardSlide == null && redCard != null)
        {
            redCardSlide = redCard.GetComponent<YellowCardSlideIn>();
        }

        if (continueButton == null)
        {
            Transform t = FindChildByName(transform, "continue_button");
            if (t == null) t = FindChildByName(transform, "Continue_button");
            if (t == null) t = FindChildByName(transform, "continueButton");
            if (t == null) t = FindChildByName(transform, "ContinueButton");
            if (t == null) t = FindChildByNamePrefixCI(transform, "continue");
            if (t == null) t = FindFirstButtonInChildren(transform);
            if (t != null) continueButton = t as RectTransform;
        }
        if (continueButton != null)
        {
            if (continueButtonComp == null) continueButtonComp = continueButton.GetComponent<Button>();
            if (continueButtonCg == null)
            {
                continueButtonCg = continueButton.GetComponent<CanvasGroup>();
                if (continueButtonCg == null) continueButtonCg = continueButton.gameObject.AddComponent<CanvasGroup>();
            }
        }
        else
        {
            Debug.LogWarning("[IncorrectWithAnswerPageAnimator] continue_button not found under '" + name + "'. Assign in inspector or rename to 'continue_button'.");
        }

        if (questionPage == null)
        {
            Transform parent = transform.parent;
            if (parent != null)
            {
                Transform sib = parent.Find("question_page");
                if (sib != null) questionPage = sib.gameObject;
            }
        }
    }

    private void Capture()
    {
        if (bg != null && bg.localScale.sqrMagnitude > 0.0001f) bgRestScale = bg.localScale;
        if (incorrectImage != null && incorrectImage.localScale.sqrMagnitude > 0.0001f) incorrectImageRestScale = incorrectImage.localScale;
        if (continueButton != null && continueButton.localScale.sqrMagnitude > 0.0001f) continueButtonRestScale = continueButton.localScale;
        if (incorrectTextCg != null && incorrectTextCg.alpha > 0f) incorrectTextRestAlpha = incorrectTextCg.alpha;
        if (answerTextCg != null && answerTextCg.alpha > 0f) answerTextRestAlpha = answerTextCg.alpha;
        if (continueButtonCg != null && continueButtonCg.alpha > 0f) continueButtonRestAlpha = continueButtonCg.alpha;
        captured = true;
    }

    private IEnumerator Play()
    {
        if (bg != null) bg.localScale = Vector3.zero;
        if (incorrectImage != null) incorrectImage.localScale = Vector3.zero;
        if (incorrectTextCg != null) incorrectTextCg.alpha = 0f;
        if (answerTextCg != null) answerTextCg.alpha = 0f;
        if (continueButton != null) continueButton.localScale = Vector3.zero;
        if (continueButtonCg != null) continueButtonCg.alpha = 0f;

        SetContinueInteractable(false);

        if (bg != null) yield return PopRT(bg, bgRestScale, bgPopDuration, bgOvershoot);
        if (questionPage != null && questionPage.activeSelf) questionPage.SetActive(false);
        if (bgToTextGap > 0f) yield return Wait(bgToTextGap);

        // Step 1.5 — Image_Incorrect (1) pops in between the bg landing and the Incorrect! text fade-in.
        if (incorrectImage != null) yield return PopRT(incorrectImage, incorrectImageRestScale, incorrectImagePopDuration, popOvershoot);
        if (incorrectImageToTextGap > 0f) yield return Wait(incorrectImageToTextGap);

        if (incorrectTextFadeDelay > 0f) yield return Wait(incorrectTextFadeDelay);
        if (incorrectTextCg != null) yield return FadeInCG(incorrectTextCg, incorrectTextRestAlpha, incorrectTextFadeDuration);
        if (incorrectToAnswerGap > 0f) yield return Wait(incorrectToAnswerGap);

        // Step 3: fire answer fade and Red_card slide in parallel, then wait for the longer one.
        Coroutine answerFade = null;
        Coroutine redSlide = null;
        if (answerTextCg != null)
        {
            answerFade = StartCoroutine(FadeInCG(answerTextCg, answerTextRestAlpha, answerTextFadeDuration));
        }
        if (redCard != null)
        {
            redCard.SetActive(true);
            float redWait = redCardWaitOverride > 0f
                ? redCardWaitOverride
                : (redCardSlide != null ? Mathf.Max(1.0f, redCardSlide.duration) : 1f);
            redSlide = StartCoroutine(Wait(redWait));
        }
        if (answerFade != null) yield return answerFade;
        if (redSlide != null) yield return redSlide;

        if (answerToContinueGap > 0f) yield return Wait(answerToContinueGap);

        if (continueButtonCg != null) continueButtonCg.alpha = continueButtonRestAlpha;
        if (continueButton != null) yield return PopRT(continueButton, continueButtonRestScale, continueButtonPopDuration, popOvershoot);

        // Re-apply every painted slot once the Red_card dissolve + continue button pop are
        // done. Heals any earlier-question Question_card colour that some part of this page
        // intro silently reset to white.
        if (QnaCardTracker.instance != null) QnaCardTracker.instance.RepaintAllPaintedSlots();

        SetContinueInteractable(true);
    }

    private void SetContinueInteractable(bool value)
    {
        if (continueButtonComp != null) continueButtonComp.interactable = value;
        if (continueButtonCg != null)
        {
            continueButtonCg.interactable = value;
            continueButtonCg.blocksRaycasts = value;
        }
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

    private IEnumerator PopRT(RectTransform rt, Vector3 restScale, float duration, float overshoot)
    {
        if (rt == null) yield break;
        rt.localScale = Vector3.zero;
        if (duration <= 0f) { rt.localScale = restScale; yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            rt.localScale = restScale * EaseOutBack(u, overshoot);
            yield return null;
        }
        rt.localScale = restScale;
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

    private static float EaseOutBack(float t, float c1)
    {
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static RectTransform FindChildRT(Transform root, string name)
    {
        Transform t = FindChildByName(root, name);
        return t != null ? t as RectTransform : null;
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

    private static Transform FindChildByNamePrefix(Transform root, string prefix)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name != null && c.name.StartsWith(prefix)) return c;
            Transform deeper = FindChildByNamePrefix(c, prefix);
            if (deeper != null) return deeper;
        }
        return null;
    }

    private static Transform FindChildByNamePrefixCI(Transform root, string prefix)
    {
        if (root == null) return null;
        string lower = prefix.ToLowerInvariant();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name != null && c.name.ToLowerInvariant().StartsWith(lower)) return c;
            Transform deeper = FindChildByNamePrefixCI(c, prefix);
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

    private static Transform FindFirstButtonInChildren(Transform root)
    {
        if (root == null) return null;
        Button b = root.GetComponentInChildren<Button>(true);
        return b != null ? b.transform : null;
    }
}

public static class IncorrectWithAnswerPageAnimatorAutoSetup
{
    private const string TargetName = "Incorrect_with_answer_page";

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
            Debug.LogWarning("[IncorrectWithAnswerPageAnimatorAutoSetup] No GameObject named '" + TargetName + "' found in the scene.");
            return;
        }
        if (go.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning("[IncorrectWithAnswerPageAnimatorAutoSetup] '" + TargetName + "' has no RectTransform; skipping.");
            return;
        }
        if (go.GetComponent<IncorrectWithAnswerPageAnimator>() == null)
        {
            go.AddComponent<IncorrectWithAnswerPageAnimator>();
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
