using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class CompletionFireworks : MonoBehaviour
{
    [Header("Condition")]
    [Tooltip("If null, auto-finds via FindObjectOfType. Fireworks only fire when GetSavedBalls() > GetMissedGoals().")]
    public game_score_manager scoreManager;

    [Header("Emission (shells)")]
    [Tooltip("Shells launched per second. Each shell rises, then bursts into colored stars.")]
    public float shellRate = 1.5f;
    [Tooltip("Optional sprite for shells/stars/sparks. Leave null for a plain colored dot.")]
    public Sprite particleSprite;

    [Header("Shell — the rising rocket")]
    [Tooltip("Diameter of the shell particle, pixels. Small.")]
    public float shellSize = 8f;
    [Tooltip("Upward launch speed, pixels/sec.")]
    public float shellLaunchSpeed = 950f;
    [Tooltip("Random +/- on launch speed.")]
    public float shellLaunchSpeedJitter = 180f;
    [Tooltip("Horizontal spread of launch direction, pixels/sec.")]
    public float shellHorizontalJitter = 120f;
    [Tooltip("Seconds until the shell bursts.")]
    public float shellFuseTime = 0.95f;
    [Tooltip("Random +/- on fuse time.")]
    public float shellFuseTimeJitter = 0.2f;
    [Tooltip("Sparks per second trailing the rising shell.")]
    public float shellTrailRate = 35f;

    [Header("Burst — the colored stars")]
    [Tooltip("Stars per burst.")]
    public int starsPerBurst = 24;
    [Tooltip("Diameter of each star, pixels.")]
    public float starSize = 7f;
    [Tooltip("Random size variation, pixels.")]
    public float starSizeJitter = 2f;
    [Tooltip("Outward radial speed for stars, pixels/sec.")]
    public float starSpeed = 360f;
    [Tooltip("Random +/- on star speed.")]
    public float starSpeedJitter = 90f;
    [Tooltip("Star lifetime, seconds.")]
    public float starLifetime = 1.25f;
    [Tooltip("Random +/- on star lifetime.")]
    public float starLifetimeJitter = 0.35f;
    [Tooltip("Per-star probability of becoming a crackle (rapid alpha flicker near end of life).")]
    [Range(0f, 1f)] public float crackleChance = 0.35f;

    [Header("Physics")]
    [Tooltip("Downward acceleration applied to shells/stars/sparks, pixels/sec^2.")]
    public float gravity = 900f;
    [Tooltip("Air drag (per second) — softens motion. 0 = no drag.")]
    [Range(0f, 4f)] public float drag = 0.6f;

    [Header("Colors")]
    [Tooltip("Burst color is picked from this palette per shell. All of that shell's stars share the color.")]
    public Color[] burstPalette = new Color[]
    {
        new Color(1f, 0.85f, 0.25f, 1f),
        new Color(1f, 0.35f, 0.45f, 1f),
        new Color(0.4f, 0.75f, 1f, 1f),
        new Color(0.65f, 1f, 0.5f, 1f),
        new Color(1f, 0.55f, 1f, 1f),
        new Color(1f, 1f, 1f, 1f)
    };
    [Tooltip("Color of the rising shell and its trail sparks (typically warm white/yellow).")]
    public Color shellColor = new Color(1f, 0.95f, 0.7f, 1f);

    [Header("Spawn Area")]
    [Range(0f, 1f)] public float spawnWidthFraction = 0.9f;
    [Tooltip("Pixels above the bottom edge where shells launch from. Ignored when Burst Above Target is set.")]
    public float spawnLineYOffset = 0f;

    [Header("Burst Position (optional)")]
    [Tooltip("If assigned (typically the score_board RectTransform), shells launch from behind this target and burst above it. Overrides fuse-based bursting + bottom-of-page spawning.")]
    public RectTransform burstAboveTarget;
    [Tooltip("Pixels above the top edge of Burst Above Target where shells burst.")]
    public float burstAboveOffsetY = 80f;
    [Tooltip("Where along Burst Above Target's height the launch line sits. 0 = the target's bottom edge, 1 = its top edge. Defaults to 1 because most scoreboards have the visible panel art at the top of an oversized rect — launching from the top edge makes shells appear from behind the visible panel rather than from the rect's bottom (which may be down near the field).")]
    [Range(0f, 1f)] public float launchFromYFraction = 1f;
    [Tooltip("Pixels below the launch reference (chosen by launchFromYFraction) where shells originate. Acts as a downward offset on that reference Y.")]
    public float launchBelowTargetY = 50f;
    [Tooltip("If true (and Burst Above Target is assigned), burst stars / trail sparks that drift BELOW the target's top edge are destroyed. Keeps the fireworks anchored above the score_board and prevents them from piling up at the lower canvas.")]
    public bool cullBelowTarget = true;

    [Header("Misc")]
    public bool useUnscaledTime = true;

    private RectTransform selfRT;
    private float shellAccumulator;
    private static Sprite fallbackSprite;

    private enum Kind { Shell, Star, Spark }

    private struct Particle
    {
        public Kind kind;
        public RectTransform rt;
        public Image img;
        public Vector2 velocity;
        public float age;
        public float life;
        public float fuse;
        public Color baseColor;
        public float trailAccumulator;
        public bool crackle;
        // Burst-by-position mode: when useTargetBurstY is true, the shell bursts as soon
        // as its anchoredPosition.y crosses targetBurstY (in addition to the fuse-time fallback).
        public float targetBurstY;
        public bool useTargetBurstY;
    }

    private readonly List<Particle> particles = new List<Particle>();

    private void Awake()
    {
        selfRT = transform as RectTransform;
        if (scoreManager == null) scoreManager = FindObjectOfType<game_score_manager>();
        // Self-find score_board if no Burst Above Target was wired up. Walks the parent
        // chain (so it works whether this WinFireworks lives under _completion_page,
        // under bg, or anywhere else in the page hierarchy). Without this, LaunchShell
        // would refuse to fire and the fireworks would silently fail to appear.
        if (burstAboveTarget == null) burstAboveTarget = TryFindScoreBoard();
    }

    private RectTransform TryFindScoreBoard()
    {
        Transform cursor = transform.parent;
        while (cursor != null)
        {
            Transform found = FindDescendantByName(cursor, "score_board");
            if (found != null) return found as RectTransform;
            cursor = cursor.parent;
        }
        return null;
    }

    private static Transform FindDescendantByName(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name) return c;
            Transform deeper = FindDescendantByName(c, name);
            if (deeper != null) return deeper;
        }
        return null;
    }

    private void OnEnable()
    {
        ClearAll();
        shellAccumulator = 0f;
    }

    private void OnDisable()
    {
        ClearAll();
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (ShouldEmit())
        {
            shellAccumulator += dt;
            float interval = shellRate > 0f ? 1f / shellRate : float.MaxValue;
            while (shellAccumulator >= interval)
            {
                shellAccumulator -= interval;
                LaunchShell();
            }
        }
        else
        {
            shellAccumulator = 0f;
        }

        StepParticles(dt);
    }

    private void StepParticles(float dt)
    {
        float dragFactor = drag > 0f ? Mathf.Exp(-drag * dt) : 1f;

        // Compute the cull line once per frame (in anchored-Y space). Any non-shell particle
        // whose anchoredPosition.y has fallen below this gets destroyed before it can drift
        // into the lower canvas area.
        bool doCull = cullBelowTarget && burstAboveTarget != null;
        float cullThresholdY = 0f;
        if (doCull)
        {
            Vector3[] corners = new Vector3[4];
            burstAboveTarget.GetWorldCorners(corners);
            cullThresholdY = WorldYToAnchoredY(corners[1].y); // target's top edge
        }

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle p = particles[i];
            if (p.rt == null) { particles.RemoveAt(i); continue; }

            p.age += dt;

            if (p.kind == Kind.Shell)
            {
                bool burstByFuse = p.age >= p.fuse;
                bool burstByHeight = p.useTargetBurstY && p.rt.anchoredPosition.y >= p.targetBurstY;
                if (burstByFuse || burstByHeight)
                {
                    Burst(p);
                    Destroy(p.rt.gameObject);
                    particles.RemoveAt(i);
                    continue;
                }
            }

            if (p.kind != Kind.Shell && p.age >= p.life)
            {
                Destroy(p.rt.gameObject);
                particles.RemoveAt(i);
                continue;
            }

            p.velocity.y -= gravity * dt;
            p.velocity *= dragFactor;
            p.rt.anchoredPosition += p.velocity * dt;

            if (p.kind == Kind.Shell)
            {
                p.trailAccumulator += dt;
                float trailInterval = shellTrailRate > 0f ? 1f / shellTrailRate : float.MaxValue;
                while (p.trailAccumulator >= trailInterval)
                {
                    p.trailAccumulator -= trailInterval;
                    SpawnSpark(p.rt.anchoredPosition, p.baseColor);
                }
                particles[i] = p;
                continue;
            }

            // Cull stars / sparks that have fallen below the score_board's top edge so
            // they don't accumulate at the bottom of the canvas.
            if (doCull && p.rt.anchoredPosition.y < cullThresholdY)
            {
                Destroy(p.rt.gameObject);
                particles.RemoveAt(i);
                continue;
            }

            float u = p.age / p.life;
            float alpha;
            if (p.crackle && u > 0.5f)
            {
                float flicker = 0.5f + 0.5f * Mathf.Sin(p.age * 60f);
                float tail = Mathf.Clamp01(1f - (u - 0.5f) / 0.5f);
                alpha = flicker * tail;
            }
            else
            {
                alpha = u < 0.65f ? 1f : Mathf.Clamp01(1f - (u - 0.65f) / 0.35f);
            }
            Color c = p.baseColor;
            c.a = p.baseColor.a * alpha;
            p.img.color = c;

            particles[i] = p;
        }
    }

    private bool ShouldEmit()
    {
        if (scoreManager == null) return false;
        return scoreManager.GetSavedBalls() > scoreManager.GetMissedGoals();
    }

    private void LaunchShell()
    {
        if (selfRT == null) return;
        // CRITICAL: refuse to launch when no target is set. Without this, the old fallback
        // path would spawn shells at the WinFireworks layer's bottom edge (= canvas bottom),
        // which is exactly the "sky shots from the bottom of the canvas" the user wants gone.
        // There is now no code path that spawns at the canvas bottom.
        if (burstAboveTarget == null) return;

        Vector3[] corners = new Vector3[4];
        burstAboveTarget.GetWorldCorners(corners);
        // corners order from GetWorldCorners: 0=BL, 1=TL, 2=TR, 3=BR
        float worldLeftX = corners[0].x;
        float worldRightX = corners[3].x;
        float worldTopY = corners[1].y;
        float worldBottomY = corners[0].y;

        // Convert target's world bounds into the WinFireworks layer's anchored-position
        // space (particles anchor at (0.5, 0), so X=0 is the layer's horizontal center
        // and Y=0 is its bottom edge).
        float leftAnchoredX = WorldXToAnchoredX(worldLeftX);
        float rightAnchoredX = WorldXToAnchoredX(worldRightX);
        // launchFromYFraction picks where along the target's height the launch line sits.
        // The default of 1 places it at the target's TOP edge — where the visible panel
        // typically lives even when the target's rect extends much further down.
        float launchRefWorldY = Mathf.Lerp(worldBottomY, worldTopY, launchFromYFraction);
        float launchAnchoredY = WorldYToAnchoredY(launchRefWorldY) - launchBelowTargetY;
        float targetBurstY = WorldYToAnchoredY(worldTopY) + burstAboveOffsetY;

        // Spawn X strictly within the target's horizontal extent so shells appear from
        // behind it. spawnWidthFraction narrows that band (1 = full target width,
        // 0.5 = central half, 0 = single column at target's center).
        float midX = (leftAnchoredX + rightAnchoredX) * 0.5f;
        float halfStrip = Mathf.Abs(rightAnchoredX - leftAnchoredX) * 0.5f * spawnWidthFraction;
        Vector2 startPos = new Vector2(midX + Random.Range(-halfStrip, halfStrip), launchAnchoredY);

        Color burstColor = (burstPalette != null && burstPalette.Length > 0)
            ? burstPalette[Random.Range(0, burstPalette.Length)]
            : Color.white;

        RectTransform rt = MakeParticleRT("Shell_runtime", shellSize, shellColor, out Image img);
        rt.anchoredPosition = startPos;

        Particle p = default;
        p.kind = Kind.Shell;
        p.rt = rt;
        p.img = img;
        p.velocity = new Vector2(
            Random.Range(-shellHorizontalJitter, shellHorizontalJitter),
            shellLaunchSpeed + Random.Range(-shellLaunchSpeedJitter, shellLaunchSpeedJitter)
        );
        p.age = 0f;
        p.fuse = Mathf.Max(0.1f, shellFuseTime + Random.Range(-shellFuseTimeJitter, shellFuseTimeJitter));
        p.baseColor = burstColor;
        p.targetBurstY = targetBurstY;
        p.useTargetBurstY = true;
        particles.Add(p);
    }

    // Converts a world-space Y into the anchoredPosition Y space used by particles
    // (their anchor is (0.5, 0) — bottom-center of selfRT). Works regardless of selfRT's
    // pivot, as long as selfRT is a child of the canvas with no rotation/scale shenanigans.
    private float WorldYToAnchoredY(float worldY)
    {
        Vector3 local = selfRT.InverseTransformPoint(new Vector3(0f, worldY, 0f));
        float bottomY = -selfRT.rect.height * selfRT.pivot.y;
        return local.y - bottomY;
    }

    // Converts a world-space X into the anchoredPosition X space used by particles
    // (anchor (0.5, 0) — horizontal center of selfRT). For the common case where
    // selfRT.pivot.x == 0.5 this is just the local X; otherwise we offset by the
    // distance between the rect's horizontal center and its pivot.
    private float WorldXToAnchoredX(float worldX)
    {
        Vector3 local = selfRT.InverseTransformPoint(new Vector3(worldX, 0f, 0f));
        float anchorX = (0.5f - selfRT.pivot.x) * selfRT.rect.width;
        return local.x - anchorX;
    }

    private void Burst(Particle shell)
    {
        Vector2 origin = shell.rt.anchoredPosition;
        Color burstColor = shell.baseColor;
        int count = Mathf.Max(1, starsPerBurst);

        float twoPi = Mathf.PI * 2f;
        float angleStep = twoPi / count;
        float angleJitter = angleStep * 0.5f;

        for (int s = 0; s < count; s++)
        {
            float angle = angleStep * s + Random.Range(-angleJitter, angleJitter);
            float speed = starSpeed + Random.Range(-starSpeedJitter, starSpeedJitter);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float sz = starSize + Random.Range(-starSizeJitter, starSizeJitter);
            if (sz < 1f) sz = 1f;

            RectTransform rt = MakeParticleRT("Star_runtime", sz, burstColor, out Image img);
            rt.anchoredPosition = origin;

            Particle p = default;
            p.kind = Kind.Star;
            p.rt = rt;
            p.img = img;
            p.velocity = dir * speed + new Vector2(0f, shell.velocity.y * 0.15f);
            p.age = 0f;
            p.life = Mathf.Max(0.1f, starLifetime + Random.Range(-starLifetimeJitter, starLifetimeJitter));
            p.baseColor = burstColor;
            p.crackle = Random.value < crackleChance;
            particles.Add(p);
        }
    }

    private void SpawnSpark(Vector2 atLocalPos, Color color)
    {
        float sz = Mathf.Max(1f, shellSize * 0.5f + Random.Range(-1f, 1f));
        RectTransform rt = MakeParticleRT("Spark_runtime", sz, color, out Image img);
        rt.anchoredPosition = atLocalPos;

        Particle p = default;
        p.kind = Kind.Spark;
        p.rt = rt;
        p.img = img;
        p.velocity = new Vector2(Random.Range(-40f, 40f), Random.Range(-80f, 20f));
        p.age = 0f;
        p.life = 0.35f + Random.Range(-0.1f, 0.1f);
        p.baseColor = color;
        particles.Add(p);
    }

    private RectTransform MakeParticleRT(string name, float size, Color color, out Image img)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(selfRT, false);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);

        img = go.AddComponent<Image>();
        img.sprite = particleSprite != null ? particleSprite : GetFallbackSprite();
        img.raycastTarget = false;
        img.color = color;
        return rt;
    }

    private void ClearAll()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i].rt != null) Destroy(particles[i].rt.gameObject);
        }
        particles.Clear();
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            fallbackSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        return fallbackSprite;
    }
}

public static class CompletionFireworksAutoSetup
{
    private const string CompletionPageName = "_completion_page";
    private const string FireworksLayerName = "WinFireworks";

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
        GameObject page = FindByName(CompletionPageName);
        if (page == null) return;

        RectTransform pageRT = page.GetComponent<RectTransform>();
        if (pageRT == null) return;

        // Recursive search — the user can keep WinFireworks under a nested container like
        // `bg` and we still find it rather than creating a duplicate at the page root.
        Transform existing = FindChildDeep(page.transform, FireworksLayerName);
        GameObject layer;
        if (existing != null)
        {
            layer = existing.gameObject;
        }
        else
        {
            layer = new GameObject(FireworksLayerName);
            RectTransform rt = layer.AddComponent<RectTransform>();
            rt.SetParent(pageRT, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        CompletionFireworks fireworks = layer.GetComponent<CompletionFireworks>();
        if (fireworks == null) fireworks = layer.AddComponent<CompletionFireworks>();

        // Recursive lookups so the auto-setup works even when the overlays are nested
        // under another container (typically `bg`).
        Transform scoreBoard = FindChildDeep(page.transform, "score_board");
        Transform playagain = FindChildDeep(page.transform, "Playagain");
        Transform leaderboard = FindChildDeep(page.transform, "Leaderboard");

        // Sibling-index reordering only makes sense when the layer shares a parent with the
        // overlay it should sit behind. If WinFireworks is under `bg` and so is score_board,
        // both share the `bg` parent — comparing/reassigning their sibling indices works.
        // If the overlays live in a different sub-tree, the indices aren't comparable and
        // we leave the layer alone (the user is then responsible for placing it correctly).
        Transform layerParent = layer.transform.parent;
        int minOverlayIdx = int.MaxValue;
        if (scoreBoard != null && scoreBoard.parent == layerParent) minOverlayIdx = Mathf.Min(minOverlayIdx, scoreBoard.GetSiblingIndex());
        if (playagain != null && playagain.parent == layerParent) minOverlayIdx = Mathf.Min(minOverlayIdx, playagain.GetSiblingIndex());
        if (leaderboard != null && leaderboard.parent == layerParent) minOverlayIdx = Mathf.Min(minOverlayIdx, leaderboard.GetSiblingIndex());

        if (minOverlayIdx != int.MaxValue)
        {
            int layerIdx = layer.transform.GetSiblingIndex();
            if (layerIdx > minOverlayIdx) layer.transform.SetSiblingIndex(minOverlayIdx);
        }

        // Auto-assign score_board as the burst target. With launchFromYFraction defaulting
        // to 1, shells now launch from behind the top of the score_board's rect (where the
        // visible panel sits) rather than from the rect's bottom edge.
        if (scoreBoard != null && fireworks.burstAboveTarget == null)
        {
            fireworks.burstAboveTarget = scoreBoard as RectTransform;
        }
    }

    private static Transform FindChildDeep(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name) return c;
            Transform deeper = FindChildDeep(c, name);
            if (deeper != null) return deeper;
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
