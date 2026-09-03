using System.Collections;
using UnityEngine;
using TMPro;

public class TimedTextFade : MonoBehaviour
{
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float visibleDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool disableAfterFadeOut = false;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Assign In Inspector")]
    public TMP_Text textText;
    private Coroutine routine;

    private void Awake()
    {
        if (textText == null)
        {
            textText = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    public void Play()
    {
        if (textText == null)
        {
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        SetAlpha(0f);
        textText.enabled = true;

        if (startDelay > 0f)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(startDelay);
            }
            else
            {
                yield return new WaitForSeconds(startDelay);
            }
        }

        yield return Fade(0f, 1f, fadeInDuration);

        if (visibleDuration > 0f)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(visibleDuration);
            }
            else
            {
                yield return new WaitForSeconds(visibleDuration);
            }
        }

        yield return Fade(1f, 0f, fadeOutDuration);

        if (disableAfterFadeOut)
        {
            textText.enabled = false;
        }

        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += DeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        if (textText == null)
        {
            return;
        }

        Color color = textText.color;
        color.a = alpha;
        textText.color = color;
    }

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

}
