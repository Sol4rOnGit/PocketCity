using UnityEngine;

public class BulletTrailFade : MonoBehaviour
{
    private float startFadeMultiplier = 2f;
    private float fadeDuration = 0.5f;
    private LineRenderer lineRenderer;
    private float timer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float ratio = timer / fadeDuration;

        float startAlpha = Mathf.Lerp(1f, 0f, ratio * startFadeMultiplier);
        float endAlpha = Mathf.Lerp(1f, 0f, ratio);

        Color startColour = lineRenderer.startColor;
        startColour.a = Mathf.Clamp01(startAlpha);
        lineRenderer.startColor = startColour;

        Color endColour = lineRenderer.endColor;
        endColour.a = Mathf.Clamp01(endAlpha);
        lineRenderer.endColor = endColour;

        if (timer >= fadeDuration) Destroy(gameObject);
    }
}
