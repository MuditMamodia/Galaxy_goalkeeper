using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_Text))]
public class HowToPlayTextAnimator : MonoBehaviour
{
    [Header("Modes (both can be true)")]
    [Tooltip("Reveal characters one by one (typewriter effect).")]
    public bool enableTyping = true;

    [Tooltip("Fade the whole text from transparent to opaque.")]
    public bool enableFade = false;

    [Header("Typing")]
    [Tooltip("Characters revealed per second when typing.")]
    public float typingCharactersPerSecond = 40f;

    [Header("Fade")]
    [Tooltip("Seconds the fade-in takes.")]
    public float fadeDuration = 0.6f;

    [Header("Skip")]
    [Tooltip("Optional. If empty, the nearest 'Continue' button up the hierarchy is used.")]
    public Button continueButton;

    [Tooltip("Use unscaled time so the animation runs while Time.timeScale is 0.")]
    public bool useUnscaledTime = true;

    private TMP_Text tmp;
    private Coroutine running;
    private float originalAlpha = 1f;
    private bool originalAlphaCaptured;
    private UnityEventCallState[] savedContinueStates;
    private bool continueGated;

    private void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        CaptureOriginalAlpha();
    }

    private void OnEnable()
    {
        if (tmp == null) tmp = GetComponent<TMP_Text>();
        CaptureOriginalAlpha();

        if (continueButton == null) continueButton = FindNearestContinueButton();
        if (continueButton != null && !continueGated)
        {
            GateContinueTransition();
            continueButton.onClick.RemoveListener(OnContinueClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        Play();
    }

    private void OnDisable()
    {
        if (running != null) { StopCoroutine(running); running = null; }
        EndGate();
    }

    private void OnContinueClicked()
    {
        if (running != null)
        {
            Skip();
        }
        else
        {
            EndGate();
        }
    }

    public void Play()
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Routine());
    }

    public void Skip()
    {
        if (running != null) { StopCoroutine(running); running = null; }
        tmp.maxVisibleCharacters = int.MaxValue;
        SetAlpha(originalAlpha);
        EndGate();
    }

    private void GateContinueTransition()
    {
        if (continueButton == null) return;
        int count = continueButton.onClick.GetPersistentEventCount();
        savedContinueStates = new UnityEventCallState[count];
        for (int i = 0; i < count; i++)
        {
            savedContinueStates[i] = continueButton.onClick.GetPersistentListenerState(i);
            continueButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }
        continueGated = true;
    }

    private void EndGate()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);

            if (savedContinueStates != null)
            {
                int n = Mathf.Min(continueButton.onClick.GetPersistentEventCount(), savedContinueStates.Length);
                for (int i = 0; i < n; i++)
                {
                    continueButton.onClick.SetPersistentListenerState(i, savedContinueStates[i]);
                }
            }
        }
        savedContinueStates = null;
        continueGated = false;
    }

    private IEnumerator Routine()
    {
        tmp.ForceMeshUpdate();
        int total = tmp.textInfo.characterCount;

        if (enableTyping) tmp.maxVisibleCharacters = 0;
        else tmp.maxVisibleCharacters = int.MaxValue;

        if (enableFade) SetAlpha(0f);
        else SetAlpha(originalAlpha);

        float typingDuration = (enableTyping && typingCharactersPerSecond > 0f)
            ? total / typingCharactersPerSecond
            : 0f;
        float maxDur = Mathf.Max(typingDuration, enableFade ? fadeDuration : 0f);
        if (maxDur <= 0f)
        {
            Skip();
            yield break;
        }

        float t = 0f;
        while (t < maxDur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (enableTyping && typingDuration > 0f)
            {
                float u = Mathf.Clamp01(t / typingDuration);
                tmp.maxVisibleCharacters = Mathf.RoundToInt(u * total);
            }
            if (enableFade && fadeDuration > 0f)
            {
                float u = Mathf.Clamp01(t / fadeDuration);
                SetAlpha(u * originalAlpha);
            }
            yield return null;
        }

        tmp.maxVisibleCharacters = int.MaxValue;
        SetAlpha(originalAlpha);
        running = null;
        EndGate();
    }

    private void CaptureOriginalAlpha()
    {
        if (originalAlphaCaptured) return;
        originalAlpha = tmp.color.a > 0f ? tmp.color.a : 1f;
        originalAlphaCaptured = true;
    }

    private void SetAlpha(float a)
    {
        Color c = tmp.color;
        c.a = a;
        tmp.color = c;
    }

    private Button FindNearestContinueButton()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            Transform found = SearchInChildren(current, "Continue");
            if (found != null && found != transform)
            {
                Button btn = found.GetComponent<Button>();
                if (btn != null) return btn;
            }
            current = current.parent;
        }
        return null;
    }

    private static Transform SearchInChildren(Transform parent, string targetName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c.name == targetName) return c;
            Transform inner = SearchInChildren(c, targetName);
            if (inner != null) return inner;
        }
        return null;
    }
}

public static class HowToPlayTextAnimatorAutoSetup
{
    private const string TargetName = "how_to_play_Text (TMP)";

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
        GameObject target = FindInSceneByName(TargetName);
        if (target == null)
        {
            Debug.LogWarning("[HowToPlayTextAnimatorAutoSetup] No GameObject named '" + TargetName + "' found in the scene.");
            return;
        }
        if (target.GetComponent<TMP_Text>() == null)
        {
            Debug.LogWarning("[HowToPlayTextAnimatorAutoSetup] '" + TargetName + "' has no TMP_Text component; skipping.");
            return;
        }
        if (target.GetComponent<HowToPlayTextAnimator>() == null)
        {
            target.AddComponent<HowToPlayTextAnimator>();
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
