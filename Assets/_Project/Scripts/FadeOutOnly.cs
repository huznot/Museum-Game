using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FadeOutOnly : MonoBehaviour
{
    public Image fadeImage;        // drag your UI Image here
    public string nextScene = "Game";

    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.5f;

    bool pressed = false;

    void Start()
    {
        // Start black → fade into menu
        StartCoroutine(Fade(1f, 0f, fadeInTime));
    }

    void Update()
    {
    }

    IEnumerator FadeAndLoad()
    {
        yield return Fade(0f, 1f, fadeOutTime);
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator Fade(float from, float to, float time)
    {
        float t = 0f;
        Color c = fadeImage.color;

        while (t < time)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / time);
            fadeImage.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }

        fadeImage.color = new Color(c.r, c.g, c.b, to);
    }
}
