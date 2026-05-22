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
    public float popInDuration = 0.22f;
    public float holdDuration = 0.15f;
    public float popOutDuration = 0.4f;
    public float overshoot = 1.7f;
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

        RectTransform rt = overlayImage.rectTransform;
        rt.localScale = Vector3.zero;
        overlayImage.color = new Color(color.r, color.g, color.b, 1f);
        overlayImage.gameObject.SetActive(true);
        overlayImage.transform.SetAsLastSibling();

        if (popInDuration > 0f)
        {
            float t = 0f;
            while (t < popInDuration)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float u = Mathf.Clamp01(t / popInDuration);
                rt.localScale = Vector3.one * EaseOutBack(u, overshoot);
                yield return null;
            }
        }
        rt.localScale = Vector3.one;

        if (holdDuration > 0f)
        {
            float t = 0f;
            while (t < holdDuration)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        if (onCovered != null) onCovered();

        if (popOutDuration > 0f)
        {
            float t = 0f;
            while (t < popOutDuration)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float u = Mathf.Clamp01(t / popOutDuration);
                float eased = Mathf.SmoothStep(0f, 1f, u);
                rt.localScale = Vector3.one * (1f - eased);
                yield return null;
            }
        }
        rt.localScale = Vector3.zero;
        overlayImage.color = new Color(color.r, color.g, color.b, 0f);
        overlayImage.gameObject.SetActive(false);
    }

    private static float EaseOutBack(float t, float c1)
    {
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}

public class QnaCardTracker : MonoBehaviour
{
    public static QnaCardTracker instance;

    [Header("Outcome Colors")]
    public Color correctFirstTryColor = new Color(0.25f, 0.85f, 0.35f, 1f);
    public Color correctSecondTryColor = new Color(1f, 0.85f, 0.1f, 1f);
    public Color failedColor = new Color(0.9f, 0.22f, 0.22f, 1f);
    public bool firstInit;

    [Serializable]
    public class Slot
    {
        public List<Image> images = new List<Image>();
        public List<Color> initialColors = new List<Color>();
    }

    [Tooltip("One slot per question. Each slot holds every Question_card image at that index across every Question_bg variant in the scene — Mark(idx, c) updates them all in sync.")]
    public Slot[] slots = new Slot[0];

    // Self-healing paint memory. Mark(idx, c) records the color here; RepaintAllPaintedSlots()
    // re-applies every recorded color, which is automatically run after every Mark and from
    // the page animators' OnEnable. Slots that got silently white-painted by some other
    // mechanism between Marks (likely Animator / CanvasGroup / sceneLoaded re-entry) are
    // restored the moment the next Mark fires OR the next page activates.
    //
    // STATIC so the paint history survives a runtime Awake re-fire (component destroyed +
    // re-added, or QnaFeedbackAutoSetup running on a re-load) — a per-instance array
    // would lose Q1's green between questions if anything re-instantiated this component.
    private const int SlotCount = 5;
    private static Color[] paintedColors;
    private static bool[] slotPainted;

    // Backwards-compat: returns one Image per slot (the first one registered). Older callers
    // that read `cards` get a flat one-per-slot view.
    public Image[] cards
    {
        get
        {
            if (slots == null) return new Image[0];
            Image[] result = new Image[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].images.Count > 0) result[i] = slots[i].images[0];
            }
            return result;
        }
    }

    private void Awake()
    {
        if (instance == null) instance = this;
         firstInit = paintedColors == null || paintedColors.Length != SlotCount;
        if (firstInit)
        {
            paintedColors = new Color[SlotCount];
            for (int i = 0; i < SlotCount; i++) paintedColors[i] = Color.white;
        }
        if (slotPainted == null || slotPainted.Length != SlotCount) slotPainted = new bool[SlotCount];

        // First-time init: force everything white (clean game-start state). Subsequent
        // Awakes (e.g. component re-added mid-game, or scene re-load with static state
        // preserved) skip the wipe and re-apply painted history instead — otherwise Q1's
        // green would silently revert to white when any later page's activation triggers
        // QnaFeedbackAutoSetup.Run → SetSlots → Awake.
        if (firstInit)
        {
            ForceAllCardsWhite();
        }
        else if (HasAnyPaintedSlot())
        {
            RepaintAllPaintedSlots();
        }
        else
        {
            ForceAllCardsWhite();
        }
    }

    private static bool HasAnyPaintedSlot()
    {
        if (slotPainted == null) return false;
        for (int i = 0; i < slotPainted.Length; i++)
        {
            if (slotPainted[i]) return true;
        }
        return false;
    }

    // Sets every registered Question_card image to white AND rewrites the slot's
    // initialColors to white, so subsequent ResetAll() calls restore to white too.
    // Also scans the loaded scene for any Question_card image whose name matches a
    // slot but wasn't registered in the cache (Question_bg variants whose hierarchy
    // diverges from the one EnsureCardTracker walks), so genuinely *every* card on
    // screen goes white — not just the ones we know about.
    private void ForceAllCardsWhite()
    {
        Debug.Log("yoyoy");

        // Final safety net: if any slot has been painted by Mark() during this play
        // session, REFUSE to wipe — re-apply the paint history instead. This catches
        // any path that reaches here mid-game (a late Awake on a host whose page only
        // activates after Q1 was already answered, a future regression, etc.).
        if (HasAnyPaintedSlot())
        {
            Debug.Log("[ForceAllCardsWhite] paint history detected — skipping wipe, repainting instead.");
            if (slots != null)
            {
                for (int s = 0; s < slots.Length; s++)
                {
                    Slot slot = slots[s];
                    if (slot == null) continue;
                    slot.initialColors.Clear();
                    for (int i = 0; i < slot.images.Count; i++)
                    {
                        slot.initialColors.Add(Color.white);
                    }
                }
            }
            RepaintAllPaintedSlots();
            return;
        }

        if (slots != null)
        {
            for (int s = 0; s < slots.Length; s++)
            {
                Slot slot = slots[s];
                if (slot == null) continue;
                slot.initialColors.Clear();
                for (int i = 0; i < slot.images.Count; i++)
                {
                    if (slot.images[i] != null) slot.images[i].color = Color.white;
                    slot.initialColors.Add(Color.white);
                }
            }
        }
        // Defensive scene-wide paint: covers every Question_card / Question_card (N)
        // even if the cache never registered it.
        for (int idx = 0; idx < 5; idx++) PaintSlotAcrossScene(idx, Color.white);
    }

    public void SetSlots(Slot[] s)
    {
        slots = s != null ? s : new Slot[0];

        // If any slot has been painted via Mark() during this play session, preserve
        // the paint history — don't wipe to white. Otherwise (clean game-start state)
        // run the original white-out + initialColors rewrite.
        if (HasAnyPaintedSlot())
        {
            // Refresh initialColors to white so ResetAll() still restores to white when
            // intentionally called (Play Again), but skip painting the live images so
            // Q1's green / Q2's yellow stay visible.
            if (slots != null)
            {
                for (int sIdx = 0; sIdx < slots.Length; sIdx++)
                {
                    Slot slot = slots[sIdx];
                    if (slot == null) continue;
                    slot.initialColors.Clear();
                    for (int i = 0; i < slot.images.Count; i++)
                    {
                        slot.initialColors.Add(Color.white);
                    }
                }
            }
            RepaintAllPaintedSlots();
        }
        else
        {
            ForceAllCardsWhite();
        }
    }

    // Re-captures initial colors from the current images. Useful if the slots were populated
    // and we want to refresh "initial" to the current Inspector colors.
    public void CaptureInitialColors()
    {
        if (slots == null) return;
        for (int s = 0; s < slots.Length; s++)
        {
            Slot slot = slots[s];
            if (slot == null) continue;
            slot.initialColors.Clear();
            for (int i = 0; i < slot.images.Count; i++)
            {
                slot.initialColors.Add(slot.images[i] != null ? slot.images[i].color : Color.white);
            }
        }
    }

    public void ResetAll()
    {
        if (slots != null)
        {
            for (int s = 0; s < slots.Length; s++)
            {
                Slot slot = slots[s];
                if (slot == null) continue;
                int n = Mathf.Min(slot.images.Count, slot.initialColors.Count);
                for (int i = 0; i < n; i++)
                {
                    if (slot.images[i] != null) slot.images[i].color = slot.initialColors[i];
                }
            }
        }
        // Defensive scene-wide reset to white. initialColors are all white after
        // ForceAllCardsWhite, so this matches the cache reset above and additionally
        // catches any Question_card the cache missed.
        for (int idx = 0; idx < 5; idx++) PaintSlotAcrossScene(idx, Color.white);
        // Clear paint memory — a full reset wipes the self-healing record too, otherwise
        // RepaintAllPaintedSlots would immediately repaint stale colors over the white.
        if (paintedColors != null)
        {
            for (int i = 0; i < paintedColors.Length; i++) paintedColors[i] = Color.white;
        }
        if (slotPainted != null)
        {
            for (int i = 0; i < slotPainted.Length; i++) slotPainted[i] = false;
        }
    }

    public void Mark(int idx, Color c)
    {
        // Defensive: initialize the static paint cache here too. Mark can fire BEFORE
        // QnaCardTracker.Awake — QnaFeedbackAutoSetup sets `instance` immediately on
        // AddComponent, but Awake doesn't run until the host GameObject becomes active.
        // If Q1's Mark(0, green) fires while the host is still inactive (the host is on
        // a page that hasn't activated yet), paintedColors is null and the cache write
        // is skipped — slotPainted[0] stays false, and the FIRST Awake (later, when that
        // page activates — e.g. when the user clicks WRONG on Q2 and incorrect_page
        // activates) sees no paint history and wipes everything to white via
        // ForceAllCardsWhite (this is the "yoyoy" log fires on Q2 wrong bug).
        if (paintedColors == null || paintedColors.Length != SlotCount)
        {
            paintedColors = new Color[SlotCount];
            for (int k = 0; k < SlotCount; k++) paintedColors[k] = Color.white;
        }
        if (slotPainted == null || slotPainted.Length != SlotCount) slotPainted = new bool[SlotCount];

        // Remember this color so RepaintAllPaintedSlots() can heal slots that get reset.
        if (idx >= 0 && idx < paintedColors.Length)
        {
            paintedColors[idx] = c;
            slotPainted[idx] = true;
        }

        int cachePainted = 0;
        if (slots != null && idx >= 0 && idx < slots.Length && slots[idx] != null)
        {
            Slot slot = slots[idx];
            for (int i = 0; i < slot.images.Count; i++)
            {
                if (slot.images[i] != null)
                {
                    slot.images[i].color = c;
                    cachePainted++;
                }
            }
        }
        else
        {
            Debug.LogWarning("[QnaCardTracker] Mark(" + idx + ") cache miss — slots="
                + (slots == null ? 0 : slots.Length) + ". Falling back to scene scan only.");
        }
        // Defensive scene-wide paint by name. Catches every Question_card / Question_card (N)
        // in the loaded scene — including any Question_bg variant the auto-setup didn't
        // register (which is the root cause of the leftmost card staying white in the user's
        // screenshot even though Mark(0, ...) was called).
        int sceneHits = PaintSlotAcrossScene(idx, c);
        Debug.Log("[QnaCardTracker] Slot " + idx + " painted (cache: " + cachePainted
            + ", scene: " + sceneHits + ") -> " + c);

        // Self-heal: re-apply EVERY previously painted slot, not just the one we just marked.
        // The user's screenshots show that some earlier slots silently revert to white between
        // Marks (likely Animator / CanvasGroup race), and this re-pass restores them so the
        // visible state finally matches the paint history.
        RepaintAllPaintedSlots();
    }

    // Public entry point — page animators call this from OnEnable so a page transition also
    // heals any slot that was reset. Cheap: 5 slots × N holders × small child walk.
    // Paints via BOTH the cached slot.images list AND a scene-wide name scan, so even if
    // one of the two paths misses a holder (e.g. a Question_bg variant whose hierarchy
    // diverges from what EnsureCardTracker registered) the painted colour still lands.
    public void RepaintAllPaintedSlots()
    {
        if (paintedColors == null || slotPainted == null) return;
        for (int i = 0; i < paintedColors.Length; i++)
        {
            if (!slotPainted[i]) continue;
            Color c = paintedColors[i];
            if (slots != null && i < slots.Length && slots[i] != null)
            {
                Slot slot = slots[i];
                for (int j = 0; j < slot.images.Count; j++)
                {
                    if (slot.images[j] != null) slot.images[j].color = c;
                }
            }
            PaintSlotAcrossScene(i, c);
        }
    }

    // Finds every Question_card_holder in any loaded scene and paints the slotIdx-th
    // Image-bearing child of each. POSITION-BASED: slot 0 = leftmost visible card under
    // each holder (sibling index 0 that has an Image), slot 1 = next, etc. This decouples
    // the slot index from the GameObject's NAME, which is the root cause of the user's
    // "first painted card lands at visual position 1" bug — the cards in the scene are
    // not in sibling order matching their names (e.g. "Question_card (1)" is at sibling
    // 0, "Question_card" is at sibling 1), so name-based lookup hits the wrong card
    // visually. Sibling-index lookup always matches what the player sees.
    private static int PaintSlotAcrossScene(int idx, Color c)
    {
        int hits = 0;
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null) continue;
            if (!go.scene.IsValid()) continue;
            if (go.hideFlags != HideFlags.None) continue;
            if (go.name != "Question_card_holder") continue;
            Image card = GetCardBySiblingIndex(go.transform, idx);
            if (card == null) continue;
            card.color = c;
            hits++;
        }
        return hits;
    }

    // Returns the slotIdx-th child of `holder` (in sibling order) that has an Image
    // component, or null if there aren't that many Image children. This is the single
    // source of truth for "what does slot N look like" across the whole script.
    public static Image GetCardBySiblingIndex(Transform holder, int slotIdx)
    {
        if (holder == null || slotIdx < 0) return null;
        int found = 0;
        for (int i = 0; i < holder.childCount; i++)
        {
            Image img = holder.GetChild(i).GetComponent<Image>();
            if (img == null) continue;
            if (found == slotIdx) return img;
            found++;
        }
        return null;
    }

    public void MarkCorrectFirstTry(int idx) { Mark(idx, correctFirstTryColor); }
    public void MarkCorrectSecondTry(int idx) { Mark(idx, correctSecondTryColor); }
    public void MarkFailed(int idx) { Mark(idx, failedColor); }
}

public static class QnaFeedbackAutoSetup
{
    private const string CardHolderName = "Question_card_holder";
    private const string QuestionBgPrefix = "Question_bg";

    private static readonly string[] CardSlotNames =
    {
        "Question_card",
        "Question_card (1)",
        "Question_card (2)",
        "Question_card (3)",
        "Question_card (4)"
    };

    private static Sprite cachedDefaultSprite;

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

    private static void EnsureCardTracker()
    {
        List<GameObject> bgs = FindAllByNamePrefix(QuestionBgPrefix);
        if (bgs.Count == 0)
        {
            Debug.LogWarning("[QnaFeedbackAutoSetup] No Question_bg* variants found.");
            return;
        }

        int slotCount = CardSlotNames.Length;
        QnaCardTracker.Slot[] slots = new QnaCardTracker.Slot[slotCount];
        for (int s = 0; s < slotCount; s++) slots[s] = new QnaCardTracker.Slot();

        Transform hostHolder = null;

        for (int b = 0; b < bgs.Count; b++)
        {
            Transform holder = FindChildDeep(bgs[b].transform, CardHolderName);
            if (holder == null) continue;
            if (hostHolder == null) hostHolder = holder;

            // POSITION-BASED registration: walk the holder's children in sibling order
            // and assign each Image-bearing child to the next available slot. slot 0 ALWAYS
            // becomes the leftmost (sibling-0) visible card under this holder, regardless
            // of the GameObject's name. This matches the rest of the painting pipeline
            // (QnaCardTracker.PaintSlotAcrossScene, GreenCardDissolve.FindTargetCard,
            // OutcomeCardDissolve.FindTargetCard) so cache and visuals never disagree.
            int registered = 0;
            for (int c = 0; c < holder.childCount && registered < slotCount; c++)
            {
                Transform childT = holder.GetChild(c);
                Image img = childT.GetComponent<Image>();
                if (img == null) continue;

                if (img.sprite == null)
                {
                    img.sprite = GetDefaultSprite();
                    img.type = Image.Type.Simple;
                    if (img.color == Color.white) img.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }

                slots[registered].images.Add(img);
                slots[registered].initialColors.Add(img.color);
                registered++;
            }
        }

        if (hostHolder == null)
        {
            Debug.LogWarning("[QnaFeedbackAutoSetup] No '" + CardHolderName + "' found under any Question_bg.");
            return;
        }

        QnaCardTracker tracker = hostHolder.GetComponent<QnaCardTracker>();
        if (tracker == null) tracker = hostHolder.gameObject.AddComponent<QnaCardTracker>();
        tracker.SetSlots(slots);
        QnaCardTracker.instance = tracker;

        int total = 0;
        for (int s = 0; s < slotCount; s++) total += slots[s].images.Count;
        Debug.Log("[QnaFeedbackAutoSetup] Card tracker initialised on '" + hostHolder.name + "' with " + total
            + " cards across " + slotCount + " slots (" + bgs.Count + " Question_bg variants found).");
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

    private static List<GameObject> FindAllByNamePrefix(string prefix)
    {
        List<GameObject> result = new List<GameObject>();
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null) continue;
            if (!go.scene.IsValid()) continue;
            if (go.hideFlags != HideFlags.None) continue;
            if (!go.name.StartsWith(prefix)) continue;
            result.Add(go);
        }
        return result;
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
}
