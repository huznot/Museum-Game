using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FadeIn : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private bool disableAfterFade = true;

    private Image fadeImage;

    private void Awake()
    {
        fadeImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (fadeImage == null)
        {
            return;
        }

        StopAllCoroutines();

        // Prevent zero duration from skipping the fade.
        float duration = Mathf.Max(0.01f, fadeDuration);

        // Ensure the image is visible before fading.
        fadeImage.enabled = true;
        SetAlpha(1f);
        fadeImage.canvasRenderer.SetAlpha(1f);

        // Built-in UI fade that respects unscaled time.
        fadeImage.CrossFadeAlpha(0f, duration, ignoreTimeScale: true);

        if (disableAfterFade)
        {
            StartCoroutine(DisableAfterFade(duration));
        }
    }

    private IEnumerator DisableAfterFade(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        fadeImage.enabled = false;
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}
