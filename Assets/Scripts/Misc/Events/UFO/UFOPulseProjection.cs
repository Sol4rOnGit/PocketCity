using System;
using System.Collections;
using UnityEngine;

public class UFOPulseProjection : MonoBehaviour
{
    [Header("Pulse")]
    [SerializeField] private GameObject pulseObject;

    [Header("Settings")]
    [SerializeField] private float startSize = 1f;
    [SerializeField] private float endSize = 50f;
    [SerializeField] private float duration = 1.2f;

    private float currentTimer = 0f;
    private float ogYScale = 0f;

    private Material pulseMaterial;
    private Color originalColor;
    public void StartPulseProjections(Action isPulseProjectionComplete)
    {
        if (pulseObject == null) return;

        StartCoroutine(UpdatePulseProjection(isPulseProjectionComplete));
    }

    private void Start()
    {
        if (pulseObject == null) return;

        ogYScale = pulseObject.transform.localScale.y;
        Renderer meshRenderer = pulseObject.GetComponent<Renderer>();

        if (meshRenderer == null) return;

        pulseMaterial = meshRenderer.material;
        originalColor = pulseMaterial.color;
    }

    private IEnumerator UpdatePulseProjection(Action isPulseProjectionComplete)
    {
        pulseObject.SetActive(true);

        while (true)
        {
            currentTimer += Time.deltaTime;
            float progress = currentTimer / duration;

            if (progress > 1)
            {
                Destroy(pulseObject);
                isPulseProjectionComplete?.Invoke();
                break;
            }

            float currentScale = Mathf.Lerp(startSize, endSize, progress);
            pulseObject.transform.localScale = new Vector3(currentScale, ogYScale, currentScale);

            if (pulseMaterial) //??
            {
                Color newColor = originalColor;
                newColor.a = Mathf.Lerp(originalColor.a, 0f, progress);
                pulseMaterial.color = newColor;
            }

            yield return null;
        }
    }
}
