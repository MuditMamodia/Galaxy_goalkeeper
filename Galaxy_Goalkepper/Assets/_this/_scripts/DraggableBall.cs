using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DraggableBall : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Optional. Leave empty to fire any Button under the pointer. " +
             "Populate to restrict drops to specific option buttons.")]
    [SerializeField] private Button[] validTargets;

    [Header("Spin")]
    public float rotationSpeed = 60f;

    [Header("Trail")]
    public bool trailEnabled = true;
    public float trailInterval = 0.04f;
    public float trailLifetime = 0.5f;
    [Range(0f, 1f)] public float trailStartAlpha = 0.55f;
    public float trailSizeFactor = 0.85f;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Image ballImage;

    private Vector2 startAnchoredPosition;
    public Vector2 HomeAnchoredPosition { get { return startAnchoredPosition; } }
    public void SetHomeAnchoredPosition(Vector2 home) { startAnchoredPosition = home; }
    private Vector2 grabOffset;
    private float trailTimer = 0f;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private readonly List<GameObject> spawnedTrailPieces = new List<GameObject>();

    private const string TrailPieceName = "BallTrail_runtime";

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        parentRect = rectTransform.parent as RectTransform;
        ballImage = GetComponent<Image>();
        startAnchoredPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        rectTransform.anchoredPosition = startAnchoredPosition;
        canvasGroup.blocksRaycasts = true;
    }

    private void OnDisable()
    {
        ClearTrail();
    }

    private void Update()
    {
        if (rotationSpeed != 0f)
        {
            rectTransform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        trailTimer = 0f;

        Vector2 localCursor;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, GetEventCamera(eventData), out localCursor))
        {
            grabOffset = rectTransform.anchoredPosition - localCursor;
        }
        else
        {
            grabOffset = Vector2.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localCursor;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, GetEventCamera(eventData), out localCursor))
        {
            rectTransform.anchoredPosition = localCursor + grabOffset;
        }

        if (trailEnabled && ballImage != null && ballImage.sprite != null)
        {
            trailTimer += Time.deltaTime;
            if (trailTimer >= trailInterval)
            {
                trailTimer = 0f;
                SpawnTrailPiece();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Button dropped = FindButtonUnderPointer(eventData);
        canvasGroup.blocksRaycasts = true;

        if (dropped != null && IsValidTarget(dropped))
        {
            // Don't snap back. Ball stays at drop position until OnEnable resets it
            // when this GameObject is re-shown (after the transition hides+reshows the page).
            dropped.onClick.Invoke();
        }
        else
        {
            rectTransform.anchoredPosition = startAnchoredPosition;
        }
    }

    private void SpawnTrailPiece()
    {
        if (parentRect == null || ballImage == null) return;

        GameObject piece = new GameObject(TrailPieceName);
        piece.transform.SetParent(parentRect, false);

        RectTransform pieceRT = piece.AddComponent<RectTransform>();
        pieceRT.anchorMin = rectTransform.anchorMin;
        pieceRT.anchorMax = rectTransform.anchorMax;
        pieceRT.pivot = rectTransform.pivot;
        pieceRT.sizeDelta = rectTransform.sizeDelta * trailSizeFactor;
        pieceRT.anchoredPosition = rectTransform.anchoredPosition;
        pieceRT.localEulerAngles = rectTransform.localEulerAngles;
        pieceRT.localScale = rectTransform.localScale;

        Image img = piece.AddComponent<Image>();
        img.sprite = ballImage.sprite;
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, trailStartAlpha);

        // Place at ball's current sibling index so the piece renders just behind the ball.
        // The ball is bumped one slot forward; newer pieces stack closest to it.
        piece.transform.SetSiblingIndex(transform.GetSiblingIndex());

        BallTrailPiece tp = piece.AddComponent<BallTrailPiece>();
        tp.lifetime = trailLifetime;
        tp.startAlpha = trailStartAlpha;

        spawnedTrailPieces.Add(piece);
    }

    private void ClearTrail()
    {
        for (int i = 0; i < spawnedTrailPieces.Count; i++)
        {
            if (spawnedTrailPieces[i] != null)
                Destroy(spawnedTrailPieces[i]);
        }
        spawnedTrailPieces.Clear();
    }

    private Button FindButtonUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null) return null;

        raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (var hit in raycastResults)
        {
            if (hit.gameObject == gameObject) continue;

            Button btn = hit.gameObject.GetComponentInParent<Button>();
            if (btn != null) return btn;
        }
        return null;
    }

    private bool IsValidTarget(Button btn)
    {
        if (validTargets == null || validTargets.Length == 0) return true;

        for (int i = 0; i < validTargets.Length; i++)
        {
            if (validTargets[i] == btn) return true;
        }
        return false;
    }

    private Camera GetEventCamera(PointerEventData eventData)
    {
        if (eventData != null && eventData.pressEventCamera != null) return eventData.pressEventCamera;
        if (canvas == null) return null;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }
}
