using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class DragAnswerAutoSetup
{
    private const string AnswerBallName = "Answer_ball";

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
        EnsureEventSystem();
        WireAnswerBall();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
        Debug.Log("[DragAnswerAutoSetup] EventSystem was missing — created one.");
    }

    private static void WireAnswerBall()
    {
        GameObject ball = FindInSceneByName(AnswerBallName);
        if (ball == null)
        {
            Debug.LogWarning("[DragAnswerAutoSetup] No GameObject named '" + AnswerBallName + "' found in the scene.");
            return;
        }

        if (ball.GetComponent<CanvasGroup>() == null)
            ball.AddComponent<CanvasGroup>();

        if (ball.GetComponent<DraggableBall>() == null)
            ball.AddComponent<DraggableBall>();
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
