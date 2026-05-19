using UnityEngine;
using UnityEngine.UI;

public class BallTrailPiece : MonoBehaviour
{
    [HideInInspector] public float lifetime = 0.5f;
    [HideInInspector] public float startAlpha = 0.55f;

    private Image img;
    private float age = 0f;

    private void Awake()
    {
        img = GetComponent<Image>();
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = age / lifetime;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        if (img != null)
        {
            Color c = img.color;
            c.a = Mathf.Lerp(startAlpha, 0f, t);
            img.color = c;
        }
    }
}
