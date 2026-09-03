using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FadeWithImage : MonoBehaviour
{
    public Image fadeImage;        // drag your UI Image here
    public string nextScene = "Game";

    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.5f;

    [Header("Menu Music")]
    public AudioSource musicSource;
    public AudioClip menuMusicClip;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    public float musicFadeOutTime = 4f;
    public bool keepMusicAcrossScenes = true;

    bool pressed = false;

    void Start()
    {
        EnsureMusicSource();

        if (musicSource != null && menuMusicClip != null)
        {
            musicSource.clip = menuMusicClip;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
            if (keepMusicAcrossScenes)
                DontDestroyOnLoad(musicSource.gameObject);
        }

        // Start black -> fade into menu
        StartCoroutine(Fade(1f, 0f, fadeInTime));
    }

    void Update()
    {
        if (pressed) return;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            pressed = true;
            StartCoroutine(FadeAndLoad());
        }
    }

    IEnumerator FadeAndLoad()
    {
        yield return Fade(0f, 1f, fadeOutTime);
        if (musicSource != null && musicSource.isPlaying && musicFadeOutTime > 0f)
            StartCoroutine(FadeMusic(musicSource, musicSource.volume, 0f, musicFadeOutTime));
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

    void EnsureMusicSource()
    {
        if (musicSource != null) return;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f; // 2D menu music
    }

    IEnumerator FadeMusic(AudioSource source, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        source.volume = to;
        if (Mathf.Approximately(to, 0f))
            source.Stop();
    }
}
