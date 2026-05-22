using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LogoShineSweep : MonoBehaviour
{
    [Header("Shine look")]
    public Color shineColor = Color.white;
    [Range(0f, 1f)] public float maxAlpha = 0.55f;
    [Tooltip("Width of the bright band in UV space (0..1 across the logo). 0.15 = ~15% of the logo width.")]
    [Range(0.02f, 0.5f)] public float thicknessUV = 0.15f;
    public float tiltDegrees = 25f;

    [Header("Timing")]
    public float sweepDuration = 2.5f;
    public float intervalBetweenSweeps = 0f;
    public float startDelay = 0f;

    private RectTransform logoRT;
    private Image logoImg;
    private RectTransform shineRT;
    private Image shineImg;
    private Material shineMaterial;
    private Coroutine routine;

    private const string LogoName = "Logo";
    private const string ShineChildName = "LogoShine_runtime";
    // Legacy names from previous versions of the script — torn down on Awake so a scene
    // saved with an older layout doesn't end up with stale GameObjects floating around.
    private const string LegacyMaskContainerName = "LogoShineMask_runtime";

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
        logoImg = GetComponent<Image>();
        StripLegacyHierarchy();
        BuildShine();
    }

    private void OnEnable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SweepLoop());
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    // -------------------------------------------------------------------------------------

    // Remove any leftovers from earlier versions of this script: Mask / RectMask2D that
    // used to live on the Logo, the legacy mask container child, or an old direct-child
    // shine that had a different material.
    private void StripLegacyHierarchy()
    {
        Mask oldMask = GetComponent<Mask>();
        if (oldMask != null)
        {
            if (Application.isPlaying) Destroy(oldMask); else DestroyImmediate(oldMask);
        }
        RectMask2D oldRectMask = GetComponent<RectMask2D>();
        if (oldRectMask != null)
        {
            if (Application.isPlaying) Destroy(oldRectMask); else DestroyImmediate(oldRectMask);
        }
        Transform oldContainer = transform.Find(LegacyMaskContainerName);
        if (oldContainer != null)
        {
            if (Application.isPlaying) Destroy(oldContainer.gameObject);
            else DestroyImmediate(oldContainer.gameObject);
        }
        Transform oldShine = transform.Find(ShineChildName);
        if (oldShine != null && oldShine.parent == transform)
        {
            if (Application.isPlaying) Destroy(oldShine.gameObject);
            else DestroyImmediate(oldShine.gameObject);
        }
    }

    private void BuildShine()
    {
        GameObject shineGo = new GameObject(ShineChildName);
        shineGo.transform.SetParent(transform, false);

        shineRT = shineGo.AddComponent<RectTransform>();
        // Match the Logo's rect exactly so the shine's UV space lines up with the logo's.
        shineRT.anchorMin = Vector2.zero;
        shineRT.anchorMax = Vector2.one;
        shineRT.offsetMin = Vector2.zero;
        shineRT.offsetMax = Vector2.zero;
        shineRT.pivot = new Vector2(0.5f, 0.5f);
        shineRT.localScale = Vector3.one;
        shineRT.localRotation = Quaternion.identity;
        shineRT.anchoredPosition = Vector2.zero;

        shineImg = shineGo.AddComponent<Image>();
        // Use the same sprite as the Logo. The shader uses tex2D(_MainTex, uv).a as the
        // logo's true alpha at each pixel, multiplied by the shine band intensity — so
        // the shine inherits the logo's exact alpha gradient and cannot bleed beyond it.
        shineImg.sprite = logoImg.sprite;
        shineImg.color = Color.white; // vertex tint; final color comes from _ShineColor on the material
        shineImg.raycastTarget = false;
        shineImg.maskable = false;
        shineImg.preserveAspect = logoImg.preserveAspect;

        // Primary path: load directly from Resources/. Anything under a Resources folder is
        // guaranteed to be packaged into the player build, so this is the one lookup that
        // CANNOT be stripped — even on WebGL with aggressive shader stripping. Shader.Find
        // is kept as an Editor-time fallback only (e.g. if the shader is mid-import).
        Shader shader = Resources.Load<Shader>("Shaders/LogoShineSweepShader");
        if (shader == null) shader = Shader.Find("UI/LogoShineSweep");
        if (shader == null)
        {
            Debug.LogWarning("[LogoShineSweep] Shader 'UI/LogoShineSweep' not found at " +
                "Resources/Shaders/LogoShineSweepShader nor via Shader.Find. Make sure the " +
                "shader asset lives under Assets/Resources/Shaders/ so it is packaged into " +
                "the build.");
            return;
        }
        shineMaterial = new Material(shader);
        shineMaterial.hideFlags = HideFlags.HideAndDontSave;
        shineMaterial.SetColor("_ShineColor", new Color(shineColor.r, shineColor.g, shineColor.b, 0f));
        shineMaterial.SetFloat("_ShineThickness", thicknessUV);
        shineMaterial.SetFloat("_ShineTilt", tiltDegrees);
        shineMaterial.SetFloat("_ShineProgress", -0.5f);
        shineImg.material = shineMaterial;
    }

    private IEnumerator SweepLoop()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        while (true)
        {
            yield return Sweep();
            yield return new WaitForSeconds(intervalBetweenSweeps);
        }
    }

    private IEnumerator Sweep()
    {
        if (shineMaterial == null) yield break;

        // Direction: top-right → bottom-left.
        // With tiltDegrees ≈ 25° the shader projects UV onto a direction that points
        // roughly toward the top-right corner of the logo, so uvProj is HIGH at the
        // top-right (~1.2) and LOW at the bottom-left (~-0.2). Starting _ShineProgress
        // beyond the top-right corner (1.3) and animating down to beyond the bottom-left
        // corner (-0.3) makes the band enter from the upper-right edge and exit through
        // the lower-left edge. To flip back to bottom-left → top-right later, just swap
        // these two constants.
        const float progressStart = 1.3f;
        const float progressEnd = -0.3f;

        float t = 0f;
        while (t < sweepDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / sweepDuration);

            // Linear progress for the band's UV position so the perceived sweep speed
            // is constant rather than ease-decelerating.
            float progress = Mathf.Lerp(progressStart, progressEnd, k);
            shineMaterial.SetFloat("_ShineProgress", progress);

            // Fade alpha in at the start and out at the end so the shine doesn't pop in
            // and out of existence at the edges.
            float alphaFactor;
            if (k < 0.15f) alphaFactor = k / 0.15f;
            else if (k > 0.85f) alphaFactor = (1f - k) / 0.15f;
            else alphaFactor = 1f;

            Color c = shineColor;
            c.a = maxAlpha * alphaFactor;
            shineMaterial.SetColor("_ShineColor", c);

            // Live-update tilt and thickness in case the developer is tweaking in the
            // Inspector during Play mode.
            shineMaterial.SetFloat("_ShineThickness", thicknessUV);
            shineMaterial.SetFloat("_ShineTilt", tiltDegrees);

            yield return null;
        }

        // End-of-sweep cleanup: hide the band fully.
        Color end = shineColor;
        end.a = 0f;
        shineMaterial.SetColor("_ShineColor", end);
    }
}
