using System.Collections;
using TMPro;
using UnityEngine;

public class floating_text : MonoBehaviour
{
    public static floating_text fp;

    [Header("Text Colors")]
    public Color perfectColor;
    public Color mediumColor;
    public Color zeroColor;

    [Header("References")]
    public TextMeshProUGUI pointsText;

    [Header("Animation Settings")]
    public float floatDistance = 120f;
    public float animationDuration = 1f;

    [Header("Scale Settings")]
    public Vector3 popScale = new Vector3(1.4f, 1.4f, 1.4f);

    private Vector3 originalPosition;
    private Vector3 originalScale;

    private Coroutine currentCoroutine;

    private void Awake()
    {
        fp = this;
    }

    void Start()
    {
        // Save original position and scale
        originalPosition = pointsText.rectTransform.localPosition;
        originalScale = pointsText.rectTransform.localScale;

        // Hide at start
        pointsText.gameObject.SetActive(false);
    }


    public void ShowPerfectPoints()
    {
        StartFloatingText("+2 Points", perfectColor);
    }

    public void ShowMediumPoints()
    {
        StartFloatingText("+1 Point", mediumColor);
    }

    public void ShowZeroPoints()
    {
        StartFloatingText("0 Point", zeroColor);
    }

    void StartFloatingText(string message, Color color)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(FloatingTextCoroutine(message, color));
    }

    IEnumerator FloatingTextCoroutine(string message, Color color)
    {
        RectTransform rect = pointsText.rectTransform;

        // Reset to original
        rect.localPosition = originalPosition;
        rect.localScale = originalScale;

        // Set text
        pointsText.text = message;
        pointsText.color = color;

        // Enable
        pointsText.gameObject.SetActive(true);

        Color startColor = color;
        float timer = 0f;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;

            float t = timer / animationDuration;

            // FLOAT UP
            rect.localPosition = Vector3.Lerp(
                originalPosition,
                originalPosition + Vector3.up * floatDistance,
                t
            );

            // POP EFFECT
            if (t < 0.2f)
            {
                rect.localScale = Vector3.Lerp(originalScale, popScale, t / 0.2f);
            }
            else
            {
                rect.localScale = Vector3.Lerp(popScale, originalScale, (t - 0.2f) / 0.8f);
            }

            // FADE OUT
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            pointsText.color = c;

            yield return null;
        }

        // Reset
        rect.localPosition = originalPosition;
        rect.localScale = originalScale;

        pointsText.gameObject.SetActive(false);
    }
}