using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BgSlideIn : MonoBehaviour
{
    [Header("Bg Slide")]
    [Tooltip("Pixels left of the rest position where the slide starts. Negative = off-screen-left.")]
    public float startOffsetX = -1500f;
    [Tooltip("Slide duration, seconds.")]
    public float duration = 0.65f;
    [Tooltip("Ease the motion out. If false, linear.")]
    public bool easeOut = true;

    [Header("Card Stagger (children of card holder under this bg)")]
    [Tooltip("Name of the child that holds the cards. Recursive search if not found at top level.")]
    public string cardHolderName = "Question_card_holder";
    [Tooltip("Pixels each card starts ABOVE its rest position before dropping.")]
    public float cardsDropDistance = 40f;
    [Tooltip("Per-card animation duration.")]
    public float cardsDropDuration = 0.76f;
    [Tooltip("Delay between consecutive cards starting.")]
    public float cardsStaggerGap = 0.1425f;
    [Tooltip("Pause between the bg landing and the first card starting.")]
    public float bgToCardsGap = 0.1425f;

    [Header("Behaviour")]
    [Tooltip("Parks + plays the full intro on every OnEnable. QuestionPageAnimator sets this " +
             "to false on question_page's Question_bg so it can schedule the intro at the right step.")]
    public bool autoPlayOnEnable = true;
    public bool useUnscaledTime = true;

    private RectTransform rt;
    private Vector2 restPosition;
    private bool restCaptured;

    private RectTransform[] cards;
    private CanvasGroup[] cardCgs;
    private Image[] cardImages;
    private Vector2[] cardRestPositions;
    private float[] cardRestAlphas;
    private float[] cardRestFill;
    private bool cardsCaptured;

    private Coroutine activeCo;

    private void Awake()
    {
        rt = (RectTransform)transform;
        CaptureIfNeeded();
    }

    private void OnEnable()
    {
        CaptureIfNeeded();
        if (autoPlayOnEnable) Play();
    }

    private void OnDisable()
    {
        if (activeCo != null) { StopCoroutine(activeCo); activeCo = null; }
    }

    public void Play()
    {
        if (!isActiveAndEnabled) return;
        if (activeCo != null) StopCoroutine(activeCo);
        activeCo = StartCoroutine(IntroCo());
    }

    public IEnumerator PlayCoroutine()
    {
        if (!isActiveAndEnabled) yield break;
        yield return IntroCo();
    }

    public void ParkOffscreen()
    {
        CaptureIfNeeded();
        rt.anchoredPosition = restPosition + new Vector2(startOffsetX, 0f);
        HideAllCards();
    }

    public void SnapToRest()
    {
        CaptureIfNeeded();
        rt.anchoredPosition = restPosition;
        ShowAllCards();
    }

    private void CaptureIfNeeded()
    {
        if (rt == null) rt = (RectTransform)transform;
        if (!restCaptured)
        {
            restPosition = rt.anchoredPosition;
            restCaptured = true;
        }
        CaptureCardsIfNeeded();
    }

    private void CaptureCardsIfNeeded()
    {
        if (cardsCaptured) return;

        Transform holder = FindCardHolder();
        if (holder == null)
        {
            cards = new RectTransform[0];
            cardCgs = new CanvasGroup[0];
            cardImages = new Image[0];
            cardRestPositions = new Vector2[0];
            cardRestAlphas = new float[0];
            cardRestFill = new float[0];
            cardsCaptured = true;
            return;
        }

        List<RectTransform> list = new List<RectTransform>();
        for (int i = 0; i < holder.childCount; i++)
        {
            RectTransform crt = holder.GetChild(i) as RectTransform;
            if (crt != null) list.Add(crt);
        }

        int n = list.Count;
        cards = list.ToArray();
        cardCgs = new CanvasGroup[n];
        cardImages = new Image[n];
        cardRestPositions = new Vector2[n];
        cardRestAlphas = new float[n];
        cardRestFill = new float[n];

        for (int i = 0; i < n; i++)
        {
            CanvasGroup cg = cards[i].GetComponent<CanvasGroup>();
            if (cg == null) cg = cards[i].gameObject.AddComponent<CanvasGroup>();
            cardCgs[i] = cg;

            cardImages[i] = cards[i].GetComponent<Image>();
            cardRestPositions[i] = cards[i].anchoredPosition;
            cardRestAlphas[i] = (cg.alpha > 0f) ? cg.alpha : 1f;

            float fill = 1f;
            Image img = cardImages[i];
            if (img != null && img.type == Image.Type.Filled && img.fillAmount > 0f) fill = img.fillAmount;
            cardRestFill[i] = fill;
        }
        cardsCaptured = true;
    }

    private Transform FindCardHolder()
    {
        if (!string.IsNullOrEmpty(cardHolderName))
        {
            Transform t = transform.Find(cardHolderName);
            if (t != null) return t;
            t = FindChildDeep(transform, cardHolderName);
            if (t != null) return t;
        }
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform c = transform.GetChild(i);
            if (c.name.IndexOf("card_holder", System.StringComparison.OrdinalIgnoreCase) >= 0) return c;
        }
        return null;
    }

    private static Transform FindChildDeep(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name) return c;
            Transform deeper = FindChildDeep(c, name);
            if (deeper != null) return deeper;
        }
        return null;
    }

    private void HideAllCards()
    {
        if (cards == null) return;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            cards[i].anchoredPosition = cardRestPositions[i] + new Vector2(0f, cardsDropDistance);
            if (cardCgs[i] != null) cardCgs[i].alpha = 0f;
            if (cardImages[i] != null && cardImages[i].type == Image.Type.Filled) cardImages[i].fillAmount = 0f;
        }
    }

    private void ShowAllCards()
    {
        if (cards == null) return;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            cards[i].anchoredPosition = cardRestPositions[i];
            if (cardCgs[i] != null) cardCgs[i].alpha = cardRestAlphas[i];
            if (cardImages[i] != null && cardImages[i].type == Image.Type.Filled) cardImages[i].fillAmount = cardRestFill[i];
        }
    }

    private IEnumerator IntroCo()
    {
        ParkOffscreen();
        yield return SlideBgCo();
        if (bgToCardsGap > 0f) yield return WaitCo(bgToCardsGap);
        yield return StaggerCardsCo();
        activeCo = null;
    }

    private IEnumerator SlideBgCo()
    {
        Vector2 startPos = restPosition + new Vector2(startOffsetX, 0f);
        float dur = Mathf.Max(0.001f, duration);
        float t = 0f;
        while (t < dur)
        {
            t += DT();
            float u = Mathf.Clamp01(t / dur);
            float eased = easeOut ? EaseOutCubic(u) : u;
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, restPosition, eased);
            yield return null;
        }
        rt.anchoredPosition = restPosition;
    }

    private IEnumerator StaggerCardsCo()
    {
        if (cards == null || cards.Length == 0) yield break;
        Coroutine[] running = new Coroutine[cards.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            float delay = i * Mathf.Max(0f, cardsStaggerGap);
            running[i] = StartCoroutine(AnimateOneCardCo(i, delay));
        }
        for (int i = 0; i < running.Length; i++) if (running[i] != null) yield return running[i];
    }

    private IEnumerator AnimateOneCardCo(int idx, float delay)
    {
        if (delay > 0f) yield return WaitCo(delay);
        if (idx < 0 || idx >= cards.Length || cards[idx] == null) yield break;

        Vector2 startPos = cardRestPositions[idx] + new Vector2(0f, cardsDropDistance);
        cards[idx].anchoredPosition = startPos;
        if (cardCgs[idx] != null) cardCgs[idx].alpha = 0f;
        if (cardImages[idx] != null && cardImages[idx].type == Image.Type.Filled) cardImages[idx].fillAmount = 0f;

        float dur = Mathf.Max(0.001f, cardsDropDuration);
        float t = 0f;
        while (t < dur)
        {
            t += DT();
            float u = Mathf.Clamp01(t / dur);
            float eased = EaseOutCubic(u);
            cards[idx].anchoredPosition = Vector2.LerpUnclamped(startPos, cardRestPositions[idx], eased);
            if (cardCgs[idx] != null) cardCgs[idx].alpha = u * cardRestAlphas[idx];
            if (cardImages[idx] != null && cardImages[idx].type == Image.Type.Filled) cardImages[idx].fillAmount = eased * cardRestFill[idx];
            yield return null;
        }
        cards[idx].anchoredPosition = cardRestPositions[idx];
        if (cardCgs[idx] != null) cardCgs[idx].alpha = cardRestAlphas[idx];
        if (cardImages[idx] != null && cardImages[idx].type == Image.Type.Filled) cardImages[idx].fillAmount = cardRestFill[idx];
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

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }
}
