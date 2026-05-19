using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LogoShineSweep : MonoBehaviour
{
    [Header("Shine look")]
    public Color shineColor = Color.white;
    [Range(0f, 1f)] public float maxAlpha = 0.55f;
    public float thickness = 350f;
    public float tiltDegrees = 25f;

    [Header("Timing")]
    public float sweepDuration = 8.0f;
    public float intervalBetweenSweeps = 0f;
    public float startDelay = 0f;

    private RectTransform logoRT;
    private RectTransform shineRT;
    private Image shineImg;
    private Coroutine routine;

    private const string LogoName = "Logo";
    private const string ShineChildName = "LogoShine_runtime";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttach()
    {
        GameObject logo = FindInSceneByName(LogoName);
        if (logo == null)
        {
            Debug.LogWarning("[LogoShineSweep] No GameObject named '" + LogoName + "' found.");
            return;
        }

        if (logo.GetComponent<Image>() == null)
        {
            Debug.LogWarning("[LogoShineSweep] '" + LogoName + "' has no Image component; skipping.");
            return;
        }

        if (logo.GetComponent<LogoShineSweep>() == null)
            logo.AddComponent<LogoShineSweep>();
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

    private void Awake()
    {
        logoRT = GetComponent<RectTransform>();
        EnsureMask();
        CreateShineChild();
    }

    private void OnEnable()
    {
        if (shineRT != null)
            shineRT.anchoredPosition = GetStartPosition();

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SweepLoop());
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    private void EnsureMask()
    {
        Mask m = GetComponent<Mask>();
        if (m == null) m = gameObject.AddComponent<Mask>();
        m.showMaskGraphic = true;
    }

    private void CreateShineChild()
    {
        Transform existing = transform.Find(ShineChildName);
        if (existing != null)
        {
            shineRT = existing as RectTransform;
            shineImg = existing.GetComponent<Image>();
            return;
        }

        GameObject go = new GameObject(ShineChildName);
        go.transform.SetParent(transform, false);
        shineRT = go.AddComponent<RectTransform>();
        shineImg = go.AddComponent<Image>();
        shineImg.raycastTarget = false;
        shineImg.maskable = true;
        shineImg.color = new Color(shineColor.r, shineColor.g, shineColor.b, 0f);
        shineImg.sprite = BuildGradientSprite(128, 4);

        shineRT.anchorMin = new Vector2(0.5f, 0.5f);
        shineRT.anchorMax = new Vector2(0.5f, 0.5f);
        shineRT.pivot = new Vector2(0.5f, 0.5f);
        shineRT.localEulerAngles = new Vector3(0f, 0f, tiltDegrees);

        float w = logoRT.rect.width;
        float h = logoRT.rect.height;
        shineRT.sizeDelta = new Vector2(thickness, Mathf.Max(w, h) * 1.8f);
    }

    private Sprite BuildGradientSprite(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            float t = (float)x / (width - 1);
            float distFromCenter = Mathf.Abs(t - 0.5f) * 2f;
            float a = 1f - distFromCenter;
            a = a * a;
            Color c = new Color(1f, 1f, 1f, a);
            for (int y = 0; y < height; y++)
                pixels[y * width + x] = c;
        }
        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private Vector2 GetStartPosition()
    {
        float w = logoRT.rect.width;
        float h = logoRT.rect.height;
        float offset = Mathf.Max(w, h);
        return new Vector2(offset, offset);
    }

    private Vector2 GetEndPosition()
    {
        return -GetStartPosition();
    }

    private IEnumerator SweepLoop()
    {
        Vector2 start = GetStartPosition();
        Vector2 end = GetEndPosition();
        shineRT.anchoredPosition = start;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        while (true)
        {
            yield return Sweep(start, end);
            shineRT.anchoredPosition = start;
            yield return new WaitForSeconds(intervalBetweenSweeps);
        }
    }

    private IEnumerator Sweep(Vector2 start, Vector2 end)
    {
        float t = 0f;
        while (t < sweepDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / sweepDuration);

            float eased = Mathf.SmoothStep(0f, 1f, k);
            shineRT.anchoredPosition = Vector2.Lerp(start, end, eased);

            float alphaFactor;
            if (k < 0.15f) alphaFactor = k / 0.15f;
            else if (k > 0.85f) alphaFactor = (1f - k) / 0.15f;
            else alphaFactor = 1f;

            shineImg.color = new Color(shineColor.r, shineColor.g, shineColor.b, maxAlpha * alphaFactor);
            yield return null;
        }
        shineImg.color = new Color(shineColor.r, shineColor.g, shineColor.b, 0f);
    }
}
