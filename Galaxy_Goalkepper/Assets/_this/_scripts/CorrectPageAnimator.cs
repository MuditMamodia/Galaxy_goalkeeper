using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class CorrectPageAnimator : MonoBehaviour
{
    [Header("References (auto-filled if left empty)")]
    public RectTransform bg;
    public RectTransform textBg;
    // Renamed from correctText → correctImage when the on-screen element changed from
    // 'correct_Text (TMP)' to 'Image_correct'. FormerlySerializedAs keeps any existing
    // scene inspector wiring intact through the rename.
    [FormerlySerializedAs("correctText")] public RectTransform correctImage;
    public CanvasGroup whyCorrectTextCg;
    public RectTransform continueButton;
    public Button continueButtonComp;
    public CanvasGroup continueButtonCg;
    public GameObject questionPage;
    [Tooltip("Drives the greencard fly + shrink + dissolve into its Question_card between text_bg pop and Image_correct pop.")]
    public GreenCardDissolve greenCardDissolve;

    [Header("Step 1 — bg pop")]
    public float bgPopDuration = 0.855f;
    public float bgToTextBgGap = 0.095f;
    [Tooltip("Bg uses its own (smaller) overshoot so the full-screen pop looks smooth.")]
    public float bgOvershoot = 0.4f;

    [Header("Step 2 — text_bg pop")]
    public float textBgPopDuration = 0.76f;
    [FormerlySerializedAs("textBgToCorrectTextGap")] public float textBgToCorrectImageGap = 0.095f;

    [Header("Step 3 — Image_correct pop")]
    [Tooltip("Pop duration (0 → resting scale) with overshoot. No rotation, no cover-scale.")]
    public float correctImagePopDuration = 0.855f;

    [Header("Step 4 — Why_correct_Text fade-in")]
    public float whyTextFadeDelay = 0.152f;
    public float whyTextFadeDuration = 1.14f;

    [Header("Step 5 — continue_button pop")]
    public float continueButtonDelay = 0.19f;
    public float continueButtonPopDuration = 0.855f;

    [Header("Easing")]
    [Tooltip("Overshoot for text_bg / continue_button pops. Lower = smoother (no bouncy snap).")]
    public float popOvershoot = 1.0f;
    public bool useUnscaledTime = true;

    

    private Vector3 bgRestScale = Vector3.one;
    private Vector3 textBgRestScale = Vector3.one;
    private Vector3 correctImageRestScale = Vector3.one;
    private Vector3 continueButtonRestScale = Vector3.one;
    private float whyTextRestAlpha = 1f;
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
        if (QnaCardTracker.instance != null) QnaCardTracker.instance.RepaintAllPaintedSlots();
        StopAllCoroutines();
        StartCoroutine(Play());
    }

    private void AutoFind()
    {
        if (bg == null) bg = FindChildRT(transform, "bg");
        if (textBg == null) textBg = FindChildRT(transform, "text_bg");
        if (correctImage == null) correctImage = FindChildRT(transform, "Image_correct");
        if (continueButton == null) continueButton = FindChildRT(transform, "continue_button");

        if (whyCorrectTextCg == null)
        {
            Transform root = textBg != null ? textBg : transform;
            Transform child = FindChildByName(root, "Why_correct_Text (TMP)");
            if (child != null)
            {
                whyCorrectTextCg = child.GetComponent<CanvasGroup>();
                if (whyCorrectTextCg == null) whyCorrectTextCg = child.gameObject.AddComponent<CanvasGroup>();
            }
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

        if (questionPage == null)
        {
            Transform parent = transform.parent;
            if (parent != null)
            {
                Transform sib = parent.Find("question_page");
                if (sib != null) questionPage = sib.gameObject;
            }
        }

        if (greenCardDissolve == null)
        {
            Transform root = textBg != null ? textBg : transform;
            Transform gc = FindChildByName(root, "greencard");
            if (gc == null) gc = FindChildByName(transform, "greencard");
            if (gc != null)
            {
                greenCardDissolve = gc.GetComponent<GreenCardDissolve>();
                if (greenCardDissolve == null && gc.GetComponent<Image>() != null)
                {
                    greenCardDissolve = gc.gameObject.AddComponent<GreenCardDissolve>();
                }
            }
        }
    }

    private void Capture()
    {
        if (bg != null && bg.localScale.sqrMagnitude > 0.0001f) bgRestScale = bg.localScale;
        if (textBg != null && textBg.localScale.sqrMagnitude > 0.0001f) textBgRestScale = textBg.localScale;
        if (correctImage != null && correctImage.localScale.sqrMagnitude > 0.0001f) correctImageRestScale = correctImage.localScale;
        if (continueButton != null && continueButton.localScale.sqrMagnitude > 0.0001f) continueButtonRestScale = continueButton.localScale;
        if (whyCorrectTextCg != null && whyCorrectTextCg.alpha > 0f) whyTextRestAlpha = whyCorrectTextCg.alpha;
        captured = true;
    }

    private IEnumerator Play()
    {
        if (bg != null) bg.localScale = Vector3.zero;
        if (textBg != null) textBg.localScale = Vector3.zero;
        if (correctImage != null) correctImage.localScale = Vector3.zero;
        if (continueButton != null) continueButton.localScale = Vector3.zero;
        if (whyCorrectTextCg != null) whyCorrectTextCg.alpha = 0f;

        SetContinueInteractable(false);

        if (bg != null) yield return PopRT(bg, bgRestScale, bgPopDuration, bgOvershoot);
        if (questionPage != null && questionPage.activeSelf) questionPage.SetActive(false);
        if (bgToTextBgGap > 0f) yield return Wait(bgToTextBgGap);

        if (textBg != null) yield return PopRT(textBg, textBgRestScale, textBgPopDuration, popOvershoot);
        if (textBgToCorrectImageGap > 0f) yield return Wait(textBgToCorrectImageGap);

        // Greencard flies + shrinks + dissolves into the Question_card before correct_Text pops up.
        if (greenCardDissolve != null) yield return greenCardDissolve.PlayCoroutine();

        // Image_correct: simple pop from 0 → resting scale with overshoot. No rotation,
        // no oversized-cover phase, no shrink. The pop is identical in feel to text_bg
        // and continue_button so the page reads as one consistent motion language.
        if (correctImage != null) yield return PopRT(correctImage, correctImageRestScale, correctImagePopDuration, popOvershoot);

        if (whyTextFadeDelay > 0f) yield return Wait(whyTextFadeDelay);
        if (whyCorrectTextCg != null) yield return FadeInCG(whyCorrectTextCg);

        if (continueButtonDelay > 0f) yield return Wait(continueButtonDelay);
        if (continueButton != null) yield return PopRT(continueButton, continueButtonRestScale, continueButtonPopDuration, popOvershoot);

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

    private IEnumerator FadeInCG(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float dur = Mathf.Max(0.001f, whyTextFadeDuration);
        float t = 0f;
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            cg.alpha = u * whyTextRestAlpha;
            yield return null;
        }
        cg.alpha = whyTextRestAlpha;
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
}

public static class CorrectPageAnimatorAutoSetup
{
    private const string TargetName = "correct_page";

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
            Debug.LogWarning("[CorrectPageAnimatorAutoSetup] No GameObject named '" + TargetName + "' found in the scene.");
            return;
        }
        if (go.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning("[CorrectPageAnimatorAutoSetup] '" + TargetName + "' has no RectTransform; skipping.");
            return;
        }
        if (go.GetComponent<CorrectPageAnimator>() == null)
        {
            go.AddComponent<CorrectPageAnimator>();
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
