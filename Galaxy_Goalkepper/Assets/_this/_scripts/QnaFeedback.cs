using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QnaFlashOverlay : MonoBehaviour
{
    public static QnaFlashOverlay instance;

    [Header("Colors")]
    public Color greenColor = new Color(0.25f, 0.85f, 0.35f, 1f);
    public Color yellowColor = new Color(1f, 0.85f, 0.1f, 1f);
    public Color redColor = new Color(0.9f, 0.22f, 0.22f, 1f);

    [Header("Timing")]
    [Tooltip("Seconds for the overlay to pop in and fully cover the screen.")]
    public float popInDuration = 0.8f;

    [Tooltip("Seconds for the overlay to fade out, revealing the new page.")]
    public float fadeOutDuration = 0.7f;

    [Tooltip("Use unscaled time so the flash runs while Time.timeScale is 0.")]
    public bool useUnscaledTime = true;

    public Image overlayImage;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public IEnumerator FlashGreen(Action onCovered = null) { return Flash(greenColor, onCovered); }
    public IEnumerator FlashYellow(Action onCovered = null) { return Flash(yellowColor, onCovered); }
    public IEnumerator FlashRed(Action onCovered = null) { return Flash(redColor, onCovered); }

    public IEnumerator Flash(Color color, Action onCovered = null)
    {
        if (overlayImage == null)
        {
            if (onCovered != null) onCovered();
            yield break;
        }

        overlayImage.gameObject.SetActive(true);
        overlayImage.transform.SetAsLastSibling();
        overlayImage.color = new Color(color.r, color.g, color.b, 0f);

        if (popInDuration > 0f)
        {
            float t = 0f;
            while (t < popInDuration)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float u = Mathf.Clamp01(t / popInDuration);
                overlayImage.color = new Color(color.r, color.g, color.b, u);
                yield return null;
            }
        }
        overlayImage.color = new Color(color.r, color.g, color.b, 1f);

        if (onCovered != null) onCovered();

        if (fadeOutDuration > 0f)
        {
            float t = 0f;
            while (t < fadeOutDuration)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float u = Mathf.Clamp01(t / fadeOutDuration);
                overlayImage.color = new Color(color.r, color.g, color.b, 1f - u);
                yield return null;
            }
        }
        overlayImage.color = new Color(color.r, color.g, color.b, 0f);
        overlayImage.gameObject.SetActive(false);
    }
}

public class QnaCardTracker : MonoBehaviour
{
    public static QnaCardTracker instance;

    [Header("Outcome Colors")]
    public Color correctFirstTryColor = new Color(0.25f, 0.85f, 0.35f, 1f);
    public Color correctSecondTryColor = new Color(1f, 0.85f, 0.1f, 1f);
    public Color failedColor = new Color(0.9f, 0.22f, 0.22f, 1f);

    public Image[] cards;

    [SerializeField] private Color[] initialColors;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void CaptureInitialColors()
    {
        if (cards == null) return;
        initialColors = new Color[cards.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null) initialColors[i] = cards[i].color;
        }
    }

    public void ResetAll()
    {
        if (cards == null || initialColors == null) return;
        int n = Mathf.Min(cards.Length, initialColors.Length);
        for (int i = 0; i < n; i++)
        {
            if (cards[i] != null) cards[i].color = initialColors[i];
        }
    }

    public void Mark(int idx, Color c)
    {
        if (cards == null)
        {
            Debug.LogWarning("[QnaCardTracker] Mark(" + idx + ") skipped — cards array is null.");
            return;
        }
        if (idx < 0 || idx >= cards.Length)
        {
            Debug.LogWarning("[QnaCardTracker] Mark(" + idx + ") skipped — out of range (cards.Length=" + cards.Length + ").");
            return;
        }
        if (cards[idx] == null)
        {
            Debug.LogWarning("[QnaCardTracker] Mark(" + idx + ") skipped — cards[" + idx + "] is null.");
            return;
        }
        cards[idx].color = c;
        Debug.Log("[QnaCardTracker] Card " + idx + " (" + cards[idx].gameObject.name + ") -> " + c);
    }

    public void MarkCorrectFirstTry(int idx) { Mark(idx, correctFirstTryColor); }
    public void MarkCorrectSecondTry(int idx) { Mark(idx, correctSecondTryColor); }
    public void MarkFailed(int idx) { Mark(idx, failedColor); }
}

public static class QnaFeedbackAutoSetup
{
    private const string CardHolderName = "Quection_card_holder";

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
        EnsureCardTracker();
        EnsureFlashOverlay();
    }

    private static readonly string[] CardNames =
    {
        "Quection_card",
        "Quection_card (1)",
        "Quection_card (2)",
        "Quection_card (3)",
        "Quection_card (4)"
    };

    private static Sprite cachedDefaultSprite;

    private static void EnsureCardTracker()
    {
        List<Image> cards = new List<Image>();
        for (int i = 0; i < CardNames.Length; i++)
        {
            GameObject go = FindByName(CardNames[i]);
            if (go == null)
            {
                Debug.LogWarning("[QnaFeedbackAutoSetup] Card '" + CardNames[i] + "' not found in scene.");
                cards.Add(null);
                continue;
            }
            Image img = go.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogWarning("[QnaFeedbackAutoSetup] Card '" + CardNames[i] + "' has no Image component.");
                cards.Add(null);
                continue;
            }

            if (img.sprite == null)
            {
                img.sprite = GetDefaultSprite();
                img.type = Image.Type.Simple;
                if (img.color == Color.white)
                {
                    img.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }

            cards.Add(img);
        }

        if (cards.Count == 0 || cards.TrueForAll(c => c == null))
        {
            Debug.LogWarning("[QnaFeedbackAutoSetup] No Quection_card images found; tracker not created.");
            return;
        }

        GameObject holder = FindByName(CardHolderName);
        if (holder == null)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && cards[i].transform.parent != null)
                {
                    holder = cards[i].transform.parent.gameObject;
                    break;
                }
            }
        }
        if (holder == null)
        {
            Debug.LogWarning("[QnaFeedbackAutoSetup] No valid host GameObject for QnaCardTracker.");
            return;
        }

        QnaCardTracker tracker = holder.GetComponent<QnaCardTracker>();
        if (tracker == null) tracker = holder.AddComponent<QnaCardTracker>();

        tracker.cards = cards.ToArray();
        tracker.CaptureInitialColors();
        QnaCardTracker.instance = tracker;

        Debug.Log("[QnaFeedbackAutoSetup] Card tracker initialised on '" + holder.name + "' with " + cards.Count + " cards.");
    }

    private static Sprite GetDefaultSprite()
    {
        if (cachedDefaultSprite != null) return cachedDefaultSprite;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        cachedDefaultSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        cachedDefaultSprite.name = "QnaCard_DefaultSprite_runtime";
        return cachedDefaultSprite;
    }

    private static void EnsureFlashOverlay()
    {
        if (QnaFlashOverlay.instance != null && QnaFlashOverlay.instance.overlayImage != null) return;

        Canvas canvas = FindUiCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[QnaFeedbackAutoSetup] No suitable Canvas found for QnaFlashOverlay.");
            return;
        }

        GameObject go = new GameObject("QnaFlashOverlay_runtime");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;

        QnaFlashOverlay overlay = go.AddComponent<QnaFlashOverlay>();
        overlay.overlayImage = img;
        QnaFlashOverlay.instance = overlay;

        go.SetActive(false);
        go.transform.SetAsLastSibling();
    }

    private static Canvas FindUiCanvas()
    {
        Canvas[] all = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < all.Length; i++)
        {
            Canvas c = all[i];
            if (!c.gameObject.scene.IsValid()) continue;
            if (c.gameObject.hideFlags != HideFlags.None) continue;
            if (!c.isRootCanvas) continue;
            if (c.name == "Canvas") return c;
        }
        for (int i = 0; i < all.Length; i++)
        {
            Canvas c = all[i];
            if (!c.gameObject.scene.IsValid()) continue;
            if (c.gameObject.hideFlags != HideFlags.None) continue;
            if (c.isRootCanvas) return c;
        }
        return null;
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
