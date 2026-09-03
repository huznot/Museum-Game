using UnityEngine;
using System.Collections;
using TMPro;

[DisallowMultipleComponent]
public class PowerFlickerLight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light targetLight;

    [Header("Objective UI")]
    [Tooltip("TMP text for 'Investigate the sound' prompt.")]
    public TMP_Text objectiveText;

    [Tooltip("Fade duration for the objective text.")]
    public float textFadeTime = 0.5f;

    [Tooltip("How long the text stays fully visible.")]
    public float textHoldTime = 3f;

    [Header("Timing")]
    [Tooltip("Base delay before flicker starts (thud timing).")]
    public float startDelay = 3.0f;

    [Tooltip("Extra random start offset per-light (breaks sync).")]
    public Vector2 startJitterRange = new Vector2(0f, 0.35f);

    [Tooltip("Total time the flicker lasts before final blackout.")]
    public float flickerDuration = 1.5f;

    [Header("Blink Pattern")]
    public Vector2 offTimeRange = new Vector2(0.22f, 0.38f);
    public Vector2 onTimeRange = new Vector2(0.05f, 0.18f);

    [Header("Behavior")]
    public bool autoStart = false;

    float originalIntensity;
    bool originalEnabled;

    System.Random rng;

    void Awake()
    {
        if (!targetLight)
            targetLight = GetComponent<Light>();

        if (targetLight)
        {
            originalIntensity = targetLight.intensity;
            originalEnabled = targetLight.enabled;
        }

        int seed = gameObject.GetInstanceID();
        rng = new System.Random(seed);

        // Ensure text starts invisible
        if (objectiveText)
        {
            Color c = objectiveText.color;
            c.a = 0f;
            objectiveText.color = c;
        }
    }

    void Start()
    {
        if (autoStart)
            StartFlicker();
    }

    public void StartFlicker()
    {
        if (!targetLight) return;
        StopAllCoroutines();
        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        float jitter = Lerp(startJitterRange.x, startJitterRange.y, Next01());
        yield return new WaitForSeconds(startDelay + jitter);

        // 🔹 Trigger objective text when flicker begins
        if (objectiveText)
            StartCoroutine(FadeObjectiveText());

        float endTime = Time.time + flickerDuration;

        targetLight.enabled = true;
        targetLight.intensity = originalIntensity;

        while (Time.time < endTime)
        {
            // ON
            targetLight.enabled = true;
            targetLight.intensity = originalIntensity;
            yield return new WaitForSeconds(Lerp(onTimeRange.x, onTimeRange.y, Next01()));

            // OFF
            targetLight.intensity = 0f;
            targetLight.enabled = false;
            yield return new WaitForSeconds(Lerp(offTimeRange.x, offTimeRange.y, Next01()));
        }

        // Final blackout
        targetLight.intensity = 0f;
        targetLight.enabled = false;
    }

    IEnumerator FadeObjectiveText()
    {
        // Fade IN
        float t = 0f;
        while (t < textFadeTime)
        {
            t += Time.deltaTime;
            SetTextAlpha(Mathf.Lerp(0f, 1f, t / textFadeTime));
            yield return null;
        }

        SetTextAlpha(1f);

        // Hold
        yield return new WaitForSeconds(textHoldTime);

        // Fade OUT
        t = 0f;
        while (t < textFadeTime)
        {
            t += Time.deltaTime;
            SetTextAlpha(Mathf.Lerp(1f, 0f, t / textFadeTime));
            yield return null;
        }

        SetTextAlpha(0f);
    }

    void SetTextAlpha(float a)
    {
        if (!objectiveText) return;
        Color c = objectiveText.color;
        c.a = a;
        objectiveText.color = c;
    }

    public void Restore()
    {
        if (!targetLight) return;
        StopAllCoroutines();
        targetLight.enabled = originalEnabled;
        targetLight.intensity = originalIntensity;
    }

    float Next01() => (float)rng.NextDouble();
    float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);
}
