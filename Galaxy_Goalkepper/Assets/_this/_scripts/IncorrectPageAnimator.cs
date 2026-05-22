using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class IncorrectPageAnimator : MonoBehaviour
{
    [Header("References (auto-filled if left empty)")]
    public RectTransform bg;
    [Tooltip("Image_Incorrect — pops in between the bg pop and the Incorrect! text fade-in.")]
    public RectTransform incorrectImage;
    public CanvasGroup incorrectTextCg;
    public GameObject yellowCard;
    public YellowCardSlideIn yellowCardSlide;
    public RectTransform continueButton;
    public Button continueButtonComp;
    public CanvasGroup continueButtonCg;
    public GameObject questionPage;

    [Header("Step 1 — bg pop")]
    public float bgPopDuration = 0.855f;
    public float bgToTextGap = 0.095f;
    [Tooltip("Bg uses its own (smaller) overshoot so the full-screen pop looks smooth.")]
    public float bgOvershoot = 0.4f;

    [Header("Step 1.5 — Image_Incorrect pop")]
    [Tooltip("Pop duration (0 → resting scale) for Image_Incorrect, with overshoot from popOvershoot. Fires after the bg-to-text gap and before the Incorrect! text fade-in.")]
    public float incorrectImagePopDuration = 0.855f;
    [Tooltip("Pause between Image_Incorrect popping in and the Incorrect! text fade-in starting.")]
    public float incorrectImageToTextGap = 0.095f;

    [Header("Step 2 — Incorrect! text fade-in")]
    public float incorrectTextFadeDelay = 0.0475f;
    public float incorrectTextFadeDuration = 0.76f;
    public float incorrectTextToYellowGap = 0.19f;

    [Header("Step 3 — yellow_card slide (uses existing YellowCardSlideIn — unchanged)")]
    [Tooltip("Seconds to wait after activating yellow_card before continuing. If <= 0, reads YellowCardSlideIn.duration automatically (with a 1s minimum).")]
    public float yellowCardWaitOverride = 0f;
    public float yellowToContinueGap = 0.5225f;

    [Header("Step 4 — continue_button pop (very last)")]
    public float continueButtonPopDuration = 0.855f;
    public float popOvershoot = 1.0f;

    [Header("Misc")]
    public bool useUnscaledTime = true;

    private Vector3 bgRestScale = Vector3.one;
    private Vector3 incorrectImageRestScale = Vector3.one;
    private Vector3 continueButtonRestScale = Vector3.one;
    private float incorrectTextRestAlpha = 1f;
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

        if (yellowCard != null && yellowCard.activeSelf) yellowCard.SetActive(false);

        if (QnaCardTracker.instance != null) QnaCardTracker.instance.RepaintAllPaintedSlots();

        StopAllCoroutines();
        StartCoroutine(Play());
    }

    private void AutoFind()
    {
        if (bg == null) bg = FindChildRT(transform, "bg");
        if (incorrectImage == null) incorrectImage = FindChildRT(transform, "Image_Incorrect");

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

        if (yellowCard == null)
        {
            Transform t = FindChildByName(transform, "yellow_card");
            if (t != null) yellowCard = t.gameObject;
        }
        if (yellowCardSlide == null && yellowCard != null)
        {
            yellowCardSlide = yellowCard.GetComponent<YellowCardSlideIn>();
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
            Debug.LogWarning("[IncorrectPageAnimator] continue_button not found under '" + name + "'. Set the reference in the inspector, or rename the button to 'continue_button'.");
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
        if (continueButtonCg != null && continueButtonCg.alpha > 0f) continueButtonRestAlpha = continueButtonCg.alpha;
        captured = true;
    }

    private IEnumerator Play()
    {
        if (bg != null) bg.localScale = Vector3.zero;
        if (incorrectImage != null) incorrectImage.localScale = Vector3.zero;
        if (incorrectTextCg != null) incorrectTextCg.alpha = 0f;
        if (continueButton != null) continueButton.localScale = Vector3.zero;
        if (continueButtonCg != null) continueButtonCg.alpha = 0f;

        SetContinueInteractable(false);

        if (bg != null) yield return PopRT(bg, bgRestScale, bgPopDuration, bgOvershoot);
        if (questionPage != null && questionPage.activeSelf) questionPage.SetActive(false);
        if (bgToTextGap > 0f) yield return Wait(bgToTextGap);

        // Step 1.5 — Image_Incorrect pops in between the bg landing and the Incorrect! text fade-in.
        if (incorrectImage != null) yield return PopRT(incorrectImage, incorrectImageRestScale, incorrectImagePopDuration, popOvershoot);
        if (incorrectImageToTextGap > 0f) yield return Wait(incorrectImageToTextGap);

        if (incorrectTextFadeDelay > 0f) yield return Wait(incorrectTextFadeDelay);
        if (incorrectTextCg != null) yield return FadeInCG(incorrectTextCg, incorrectTextRestAlpha, incorrectTextFadeDuration);
        if (incorrectTextToYellowGap > 0f) yield return Wait(incorrectTextToYellowGap);

        if (yellowCard != null)
        {
            yellowCard.SetActive(true);
            float wait = yellowCardWaitOverride > 0f
                ? yellowCardWaitOverride
                : (yellowCardSlide != null ? Mathf.Max(1.0f, yellowCardSlide.duration) : 1f);
            yield return Wait(wait);
        }
        if (yellowToContinueGap > 0f) yield return Wait(yellowToContinueGap);

        if (continueButtonCg != null) continueButtonCg.alpha = continueButtonRestAlpha;
        if (continueButton != null) yield return PopRT(continueButton, continueButtonRestScale, continueButtonPopDuration, popOvershoot);

        // Re-apply every painted slot once the yellow_card dissolve + continue button pop
        // are done. Heals any earlier-question Question_card colour that some part of the
        // incorrect_page intro silently reset to white.
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

    private static Transform FindFirstButtonInChildren(Transform root)
    {
        if (root == null) return null;
        Button b = root.GetComponentInChildren<Button>(true);
        return b != null ? b.transform : null;
    }
}

public static class IncorrectPageAnimatorAutoSetup
{
    private const string TargetName = "Incorrect_page";

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
            Debug.LogWarning("[IncorrectPageAnimatorAutoSetup] No GameObject named '" + TargetName + "' found in the scene.");
            return;
        }
        if (go.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning("[IncorrectPageAnimatorAutoSetup] '" + TargetName + "' has no RectTransform; skipping.");
            return;
        }
        if (go.GetComponent<IncorrectPageAnimator>() == null)
        {
            go.AddComponent<IncorrectPageAnimator>();
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
