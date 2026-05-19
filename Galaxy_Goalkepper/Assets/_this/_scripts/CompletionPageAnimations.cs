using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RectTransform))]
public class ScalePopOnEnable : MonoBehaviour
{
    [Tooltip("Total time of the pop, in seconds.")]
    public float duration = 0.6f;

    [Tooltip("How much the scale overshoots before settling. 0 = no overshoot.")]
    public float overshoot = 1.7f;

    [Tooltip("Wait until transition_effects.IsBusy becomes false before starting.")]
    public bool waitForTransition;

    [Tooltip("Use unscaled time so the animation runs while Time.timeScale is 0.")]
    public bool useUnscaledTime = true;

    private RectTransform rt;
    private Vector3 restingScale = Vector3.one;
    private bool restingCaptured;

    private void Awake()
    {
        Capture();
    }

    private void OnEnable()
    {
        if (!restingCaptured) Capture();
        rt.localScale = Vector3.zero;
        StopAllCoroutines();
        StartCoroutine(Routine());
    }

    private void Capture()
    {
        rt = GetComponent<RectTransform>();
        Vector3 s = rt.localScale;
        if (s.sqrMagnitude > 0.0001f) restingScale = s;
        restingCaptured = true;
    }

    private IEnumerator Routine()
    {
        rt.localScale = Vector3.zero;

        while (waitForTransition
               && transition_effects.instance != null
               && transition_effects.instance.IsBusy)
        {
            yield return null;
        }

        if (duration <= 0f)
        {
            rt.localScale = restingScale;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            rt.localScale = restingScale * EaseOutBack(u, overshoot);
            yield return null;
        }

        rt.localScale = restingScale;
    }

    private static float EaseOutBack(float t, float c1)
    {
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}

public class FadeInOnEnable : MonoBehaviour
{
    [Tooltip("Seconds to wait before fading starts.")]
    public float startDelay = 0.6f;

    [Tooltip("Seconds the fade-in takes.")]
    public float duration = 0.5f;

    [Tooltip("Wait until transition_effects.IsBusy becomes false before starting (delay/fade timer is counted from there).")]
    public bool waitForTransition;

    [Tooltip("Use unscaled time so the animation runs while Time.timeScale is 0.")]
    public bool useUnscaledTime = true;

    private CanvasGroup cg;
    private float originalAlpha = 1f;
    private bool originalCaptured;

    private void Awake()
    {
        Capture();
    }

    private void OnEnable()
    {
        if (!originalCaptured) Capture();
        StopAllCoroutines();
        StartCoroutine(Routine());
    }

    private void Capture()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        if (cg.alpha > 0f) originalAlpha = cg.alpha;
        originalCaptured = true;
    }

    private IEnumerator Routine()
    {
        cg.alpha = 0f;

        while (waitForTransition
               && transition_effects.instance != null
               && transition_effects.instance.IsBusy)
        {
            yield return null;
        }

        float wait = 0f;
        while (wait < startDelay)
        {
            wait += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        if (duration <= 0f)
        {
            cg.alpha = originalAlpha;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            cg.alpha = u * originalAlpha;
            yield return null;
        }

        cg.alpha = originalAlpha;
    }
}

[RequireComponent(typeof(RectTransform))]
public class BobOnLoop : MonoBehaviour
{
    [Tooltip("Peak pixel offset from the resting position (up and down).")]
    public float amplitude = 15f;

    [Tooltip("Seconds for one full up-down cycle. Larger = slower.")]
    public float period = 3f;

    [Tooltip("Seconds to wait after enable before bobbing starts. Useful when paired with a pop-in.")]
    public float startDelay = 0.6f;

    [Tooltip("Use unscaled time so the bob runs while Time.timeScale is 0.")]
    public bool useUnscaledTime = true;

    private RectTransform rt;
    private Vector2 restingPos;
    private bool restingCaptured;
    private float phase;

    private void Awake()
    {
        Capture();
    }

    private void OnEnable()
    {
        if (!restingCaptured) Capture();
        phase = -startDelay;
        rt.anchoredPosition = restingPos;
    }

    private void Capture()
    {
        rt = GetComponent<RectTransform>();
        restingPos = rt.anchoredPosition;
        restingCaptured = true;
    }

    private void Update()
    {
        phase += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (phase < 0f || period <= 0f) return;

        float offsetY = Mathf.Sin((phase / period) * Mathf.PI * 2f) * amplitude;
        rt.anchoredPosition = restingPos + new Vector2(0f, offsetY);
    }
}

public static class CompletionPageAnimationsAutoSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        Run();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Run();
    }

    private static void Run()
    {
        AttachScoreBoardPop("score_board");
        AttachScoreFade("home_score", 0.55f);
        AttachScoreFade("away_score", 0.7f);

        AttachLogo("Logo");
    }

    private static void AttachScoreBoardPop(string name)
    {
        GameObject go = FindByName(name);
        if (go == null)
        {
            Debug.LogWarning("[CompletionPageAnimationsAutoSetup] No GameObject named '" + name + "' found.");
            return;
        }
        if (go.GetComponent<RectTransform>() == null) return;
        if (go.GetComponent<ScalePopOnEnable>() == null)
        {
            ScalePopOnEnable pop = go.AddComponent<ScalePopOnEnable>();
            pop.waitForTransition = true;
        }
    }

    private static void AttachScoreFade(string name, float delay)
    {
        GameObject go = FindByName(name);
        if (go == null)
        {
            Debug.LogWarning("[CompletionPageAnimationsAutoSetup] No GameObject named '" + name + "' found.");
            return;
        }
        if (go.GetComponent<FadeInOnEnable>() == null)
        {
            FadeInOnEnable fi = go.AddComponent<FadeInOnEnable>();
            fi.startDelay = delay;
            fi.waitForTransition = true;
        }
    }

    private static void AttachLogo(string name)
    {
        GameObject go = FindByName(name);
        if (go == null)
        {
            Debug.LogWarning("[CompletionPageAnimationsAutoSetup] No GameObject named '" + name + "' found.");
            return;
        }
        if (go.GetComponent<RectTransform>() == null) return;

        if (go.GetComponent<ScalePopOnEnable>() == null)
        {
            ScalePopOnEnable pop = go.AddComponent<ScalePopOnEnable>();
            pop.duration = 0.6f;
            pop.overshoot = 1.7f;
            pop.waitForTransition = false;
        }

        if (go.GetComponent<BobOnLoop>() == null)
        {
            BobOnLoop bob = go.AddComponent<BobOnLoop>();
            bob.amplitude = 15f;
            bob.period = 3f;
            bob.startDelay = 0.6f;
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
