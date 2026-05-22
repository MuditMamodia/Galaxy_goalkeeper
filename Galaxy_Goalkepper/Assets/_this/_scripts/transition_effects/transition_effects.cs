using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class transition_effects : MonoBehaviour
{
    public static transition_effects instance;

    [HideInInspector] public bool IsBusy = false;

    [Header("Images")]
    public Image fadeImage;
    public Image footballImage;

    [Header("Football Positions (Set in Inspector)")]
    public RectTransform footballRect;
    public RectTransform leftPos;
    public RectTransform centerPos;
    public RectTransform rightPos;

    [Header("Settings")]
    public float fadeSpeed = 2f;
    public float slideSpeed = 2f;
    public float rotationSpeed = 200f;

    private void Awake()
    {
        instance = this;
        //StartCoroutine(FadeIn());
        SetAlpha(0);
    }

    // ---------------- FADE ----------------
    public IEnumerator FadeIn()
    {
        fadeImage.raycastTarget = true;
        float t = 1f;

        while (t > 0)
        {
            t -= Time.deltaTime * fadeSpeed;
            SetAlpha(t);
            yield return null;
        }

        SetAlpha(0);
        fadeImage.raycastTarget = false;
    }

    public IEnumerator FadeOut()
    {
        fadeImage.raycastTarget = true;
        float t = 0f;

        while (t < 1)
        {
            t += Time.deltaTime * fadeSpeed;
            SetAlpha(t);
            yield return null;
        }

        SetAlpha(1);
        fadeImage.raycastTarget = true;
    }

    void SetAlpha(float a)
    {
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }

    // ---------------- FOOTBALL: LEFT → CENTER ----------------
    public IEnumerator FootballSlideIn()
    {
        float t = 0f;
        footballImage.raycastTarget = true;

        footballRect.anchoredPosition = leftPos.anchoredPosition;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;

            footballRect.anchoredPosition =
                Vector2.Lerp(leftPos.anchoredPosition, centerPos.anchoredPosition, t);

            footballRect.Rotate(0, 0, -rotationSpeed * Time.deltaTime);

            yield return null;
        }

        footballRect.anchoredPosition = centerPos.anchoredPosition;
    }

    // ---------------- FOOTBALL: CENTER → RIGHT ----------------
    public IEnumerator FootballSlideOut()
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;

            footballRect.anchoredPosition =
                Vector2.Lerp(centerPos.anchoredPosition, rightPos.anchoredPosition, t);

            footballRect.Rotate(0, 0, -rotationSpeed * Time.deltaTime);

            yield return null;
        }
        footballImage.raycastTarget = false;
        footballRect.anchoredPosition = rightPos.anchoredPosition;
    }

    // ---------------- FULL TRANSITION ----------------
    public IEnumerator FootballFullTransition()
    {
        yield return StartCoroutine(FootballSlideIn());
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FootballSlideOut());
    }
}