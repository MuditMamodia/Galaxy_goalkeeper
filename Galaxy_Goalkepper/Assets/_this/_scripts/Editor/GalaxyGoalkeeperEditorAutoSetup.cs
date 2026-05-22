#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Runs in the Unity Editor BEFORE the user hits Play.
// Walks every open scene and makes sure QuestionPageAnimator is on every
// "question_page" GameObject, and that "_completion_page" has a "WinFireworks"
// child with CompletionFireworks on it. Re-runs on assembly reload and when
// any scene is opened. Marks scenes dirty only when an actual change was made.
public static class GalaxyGoalkeeperEditorAutoSetup
{
    private const string QuestionPageName = "question_page";
    private const string CompletionPageName = "_completion_page";
    private const string FireworksLayerName = "WinFireworks";
    private const string QuestionBgPrefix = "Question_bg";
    private const string CardHolderName = "Question_card_holder";
    private static readonly string[] OutcomeCardContainerNames = { "yellow_card", "Red_card" };
    // The child holding the visible card image. yellow_card uses "card"; Red_card uses "card (1)".
    private static readonly string[] OutcomeCardChildNames = { "card", "card (1)" };

    private static readonly string[] CardSlotNames =
    {
        "Question_card",
        "Question_card (1)",
        "Question_card (2)",
        "Question_card (3)",
        "Question_card (4)"
    };

    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.delayCall += ApplyToAllOpenScenes;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        ApplyToScene(scene);
    }

    [MenuItem("Tools/Galaxy Goalkeeper/Apply Auto-Setup To Open Scenes")]
    public static void ApplyToAllOpenScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            ApplyToScene(s);
        }
    }

    private static void ApplyToScene(Scene scene)
    {
        bool changed = false;
        List<Transform> bgs = new List<Transform>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in EnumerateDeep(root.transform))
            {
                if (t.name == QuestionPageName)
                {
                    changed |= EnsureQuestionPageAnimator(t);
                }
                else if (t.name == CompletionPageName)
                {
                    changed |= EnsureCompletionFireworks(t);
                }
                if (t.name.StartsWith(QuestionBgPrefix))
                {
                    changed |= EnsureBgSlideIn(t);
                    bgs.Add(t);
                }
                if (IsOutcomeCardContainer(t.name))
                {
                    changed |= EnsureOutcomeCardDissolve(t);
                }
            }
        }

        if (bgs.Count > 0)
        {
            changed |= EnsureQnaCardTracker(bgs);
        }

        if (changed && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    private static bool EnsureQuestionPageAnimator(Transform pageT)
    {
        if (pageT.GetComponent<RectTransform>() == null) return false;
        if (pageT.GetComponent<QuestionPageAnimator>() != null) return false;
        Undo.AddComponent<QuestionPageAnimator>(pageT.gameObject);
        return true;
    }

    private static bool EnsureBgSlideIn(Transform bgT)
    {
        if (bgT.GetComponent<RectTransform>() == null) return false;
        BgSlideIn helper = bgT.GetComponent<BgSlideIn>();
        bool changed = false;
        if (helper == null)
        {
            helper = Undo.AddComponent<BgSlideIn>(bgT.gameObject);
            changed = true;
        }

        // The Question_bg directly under question_page is driven by QuestionPageAnimator
        // (it triggers the slide at the right step of its larger sequence). All others
        // run on their own when their page activates.
        bool insideQuestionPage = HasAncestorNamed(bgT, QuestionPageName);
        bool wantAuto = !insideQuestionPage;
        if (helper.autoPlayOnEnable != wantAuto)
        {
            Undo.RecordObject(helper, "Configure BgSlideIn");
            helper.autoPlayOnEnable = wantAuto;
            changed = true;
        }
        return changed;
    }

    private static bool EnsureQnaCardTracker(List<Transform> bgs)
    {
        if (bgs == null || bgs.Count == 0) return false;

        QnaCardTracker.Slot[] slots = new QnaCardTracker.Slot[CardSlotNames.Length];
        for (int i = 0; i < slots.Length; i++) slots[i] = new QnaCardTracker.Slot();

        Transform hostHolder = null;

        for (int b = 0; b < bgs.Count; b++)
        {
            Transform holder = FindChildDeep(bgs[b], CardHolderName);
            if (holder == null) continue;
            if (hostHolder == null) hostHolder = holder;

            // Position-based: walk holder children in sibling order and register Image-bearing
            // children to consecutive slots. Matches the runtime registration in
            // QnaFeedbackAutoSetup.EnsureCardTracker so the baked scene cache agrees with
            // what the runtime sees — and with what the player sees.
            int registered = 0;
            for (int c = 0; c < holder.childCount && registered < CardSlotNames.Length; c++)
            {
                Transform childT = holder.GetChild(c);
                Image img = childT.GetComponent<Image>();
                if (img == null) continue;

                slots[registered].images.Add(img);
                slots[registered].initialColors.Add(img.color);
                registered++;
            }
        }

        if (hostHolder == null) return false;

        QnaCardTracker tracker = hostHolder.GetComponent<QnaCardTracker>();
        bool changed = false;
        if (tracker == null)
        {
            tracker = Undo.AddComponent<QnaCardTracker>(hostHolder.gameObject);
            changed = true;
        }

        if (!SameSlots(tracker.slots, slots))
        {
            Undo.RecordObject(tracker, "Populate QnaCardTracker slots");
            tracker.slots = slots;
            changed = true;
        }
        return changed;
    }

    private static bool SameSlots(QnaCardTracker.Slot[] a, QnaCardTracker.Slot[] b)
    {
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] == null || b[i] == null) return false;
            if (a[i].images == null || b[i].images == null) return false;
            if (a[i].images.Count != b[i].images.Count) return false;
            for (int j = 0; j < a[i].images.Count; j++)
            {
                if (a[i].images[j] != b[i].images[j]) return false;
            }
        }
        return true;
    }

    private static bool IsOutcomeCardContainer(string name)
    {
        for (int i = 0; i < OutcomeCardContainerNames.Length; i++)
        {
            if (OutcomeCardContainerNames[i] == name) return true;
        }
        return false;
    }

    private static bool EnsureOutcomeCardDissolve(Transform containerT)
    {
        Transform cardT = null;
        for (int i = 0; i < OutcomeCardChildNames.Length; i++)
        {
            cardT = containerT.Find(OutcomeCardChildNames[i]);
            if (cardT != null) break;
        }
        if (cardT == null) return false;
        if (cardT.GetComponent<Image>() == null) return false;
        if (cardT.GetComponent<OutcomeCardDissolve>() != null) return false;

        Undo.AddComponent<OutcomeCardDissolve>(cardT.gameObject);
        return true;
    }

    private static bool HasAncestorNamed(Transform t, string name)
    {
        Transform p = t.parent;
        while (p != null)
        {
            if (p.name == name) return true;
            p = p.parent;
        }
        return false;
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

    private static bool EnsureCompletionFireworks(Transform pageT)
    {
        RectTransform pageRT = pageT.GetComponent<RectTransform>();
        if (pageRT == null) return false;

        bool changed = false;
        Transform layer = pageT.Find(FireworksLayerName);
        if (layer == null)
        {
            GameObject go = new GameObject(FireworksLayerName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create WinFireworks layer");
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(pageRT, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsLastSibling();
            layer = rt;
            changed = true;
        }
        if (layer.GetComponent<CompletionFireworks>() == null)
        {
            Undo.AddComponent<CompletionFireworks>(layer.gameObject);
            changed = true;
        }
        return changed;
    }

    private static IEnumerable<Transform> EnumerateDeep(Transform root)
    {
        Stack<Transform> stack = new Stack<Transform>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            Transform t = stack.Pop();
            yield return t;
            for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
        }
    }
}
#endif
