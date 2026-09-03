using System.Collections;
using TMPro;
using UnityEngine;

public class OutOfFuelTextSequence : MonoBehaviour
{
    [Header("References")]
    public CarController carController;
    public TMP_Text firstText;
    public TMP_Text secondText;
    public TMP_Text thirdText;

    [Header("Timing")]
    public float delayBeforeFirst = 1f;
    public float firstFadeInDuration = 0.4f;
    public float firstFadeOutDuration = 0.4f;
    public float secondFadeInDuration = 0.4f;
    public float secondFadeOutDuration = 0.4f;
    public float thirdFadeInDuration = 0.4f;
    public float thirdVisibleDuration = 2f;
    public float thirdFadeOutDuration = 0.4f;
    public float stopSpeedThreshold = 0.1f;
    public bool useUnscaledTime = true;

    private bool started;
    private Coroutine routine;

    private void Awake()
    {
        if (carController == null)
        {
            carController = GetComponentInParent<CarController>();
        }
    }

    private void OnEnable()
    {
        PrepareText(firstText);
        PrepareText(secondText);
        PrepareText(thirdText);
    }

    private void Update()
    {
        if (started)
        {
            return;
        }

        if (carController != null && carController.IsOutOfFuel())
        {
            started = true;
            routine = StartCoroutine(RunSequence());
        }
    }

    private IEnumerator RunSequence()
    {
        if (delayBeforeFirst > 0f)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(delayBeforeFirst);
            }
            else
            {
                yield return new WaitForSeconds(delayBeforeFirst);
            }
        }

        yield return FadeText(firstText, 0f, 1f, firstFadeInDuration, enableDuringFade: true);

        while (carController != null && carController.GetSpeed() > stopSpeedThreshold)
        {
            yield return null;
        }

        yield return FadeText(firstText, 1f, 0f, firstFadeOutDuration, enableDuringFade: false);

        yield return FadeText(secondText, 0f, 1f, secondFadeInDuration, enableDuringFade: true);

        while (true)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                break;
            }
            yield return null;
        }

        yield return FadeText(secondText, 1f, 0f, secondFadeOutDuration, enableDuringFade: false);

        yield return FadeText(thirdText, 0f, 1f, thirdFadeInDuration, enableDuringFade: true);

        if (thirdVisibleDuration > 0f)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(thirdVisibleDuration);
            }
            else
            {
                yield return new WaitForSeconds(thirdVisibleDuration);
            }
        }

        yield return FadeText(thirdText, 1f, 0f, thirdFadeOutDuration, enableDuringFade: false);
        routine = null;
    }

    private IEnumerator FadeText(TMP_Text text, float from, float to, float duration, bool enableDuringFade)
    {
        if (text == null)
        {
            yield break;
        }

        if (enableDuringFade)
        {
            text.enabled = true;
        }

        if (duration <= 0f)
        {
            SetAlpha(text, to);
            if (!enableDuringFade)
            {
                text.enabled = false;
            }
            yield break;
        }

        float elapsed = 0f;
        SetAlpha(text, from);

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(text, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(text, to);
        if (!enableDuringFade)
        {
            text.enabled = false;
        }
    }

    private void PrepareText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.enabled = false;
        SetAlpha(text, 0f);
    }

    private void SetAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }
}
