using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RectTransform))]
public class YellowCardSlideIn : MonoBehaviour
{
    [Tooltip("Pixels to start to the LEFT of the resting position. Positive value = starts further off-screen left.")]
    public float slideDistance = 1500f;

    [Tooltip("How long the slide takes, in seconds.")]
    public float duration = 1f;

    [Tooltip("Easing curve. Input 0..1, output 0..1.")]
    public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Use unscaled time so the slide runs even when Time.timeScale is 0.")]
    public bool useUnscaledTime = true;

    private RectTransform rt;
    private Vector2 restingPosition;
    private bool restingCaptured;

    private void Awake()
    {
        CaptureResting();
    }

    private void OnEnable()
    {
        if (!restingCaptured) CaptureResting();

        // Check object name. Null-guard floating_points.fp — its Awake/instance may not
        // have run yet when the page first activates (script execution order race), and
        // we don't want OnEnable to throw before the slide-in coroutine starts.
       

        StopAllCoroutines();
        StartCoroutine(SlideRoutine());
    }

    private void CaptureResting()
    {
        rt = GetComponent<RectTransform>();
        restingPosition = rt.anchoredPosition;
        restingCaptured = true;
    }

    private IEnumerator SlideRoutine()
    {
        Vector2 startPosition = restingPosition + new Vector2(-Mathf.Abs(slideDistance), 0f);
        rt.anchoredPosition = startPosition;

        float t = 0f;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            float eased = easing.Evaluate(u);
            rt.anchoredPosition = Vector2.LerpUnclamped(startPosition, restingPosition, eased);
            yield return null;
        }

        rt.anchoredPosition = restingPosition;
    }
}

public static class YellowCardSlideInAutoSetup
{
    private static readonly string[] TargetNames = { "yellow_card", "Red_card" };

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
        for (int i = 0; i < TargetNames.Length; i++)
        {
            AttachTo(TargetNames[i]);
        }
    }

    private static void AttachTo(string targetName)
    {
        GameObject target = FindInSceneByName(targetName);
        if (target == null)
        {
            Debug.LogWarning("[YellowCardSlideInAutoSetup] No GameObject named '" + targetName + "' found in the scene.");
            return;
        }

        if (target.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning("[YellowCardSlideInAutoSetup] '" + targetName + "' has no RectTransform; skipping.");
            return;
        }

        if (target.GetComponent<YellowCardSlideIn>() == null)
        {
            target.AddComponent<YellowCardSlideIn>();
        }
    }

    private static GameObject FindInSceneByName(string targetName)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go.name != targetName) continue;
            if (!go.scene.IsValid()) continue;
            if (go.hideFlags != HideFlags.None) continue;
            return go;
        }
        return null;
    }
}
