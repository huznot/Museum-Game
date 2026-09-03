using System.Collections;
using UnityEngine;

public class CarTriggerEnabler : MonoBehaviour
{
    [Header("Trigger Enable")]
    [Tooltip("Trigger collider to detect touch.")]
    public Collider targetTrigger;
    [Tooltip("Object to enable when this collider touches the target trigger.")]
    public GameObject objectToEnable;
    [Tooltip("Only enable once.")]
    public bool enableOnce = true;
    [Tooltip("If true, sets Act3DialogueGate when this trigger is touched.")]
    public bool setAct3DialogueGate = false;

    [Header("Music Trigger")]
    [Tooltip("Separate trigger collider that starts music when touched.")]
    public Collider musicTrigger;
    [Tooltip("AudioSource used to play the trigger music.")]
    public AudioSource musicSource;
    [Tooltip("Auto-create an AudioSource for music if none is assigned.")]
    public bool autoCreateMusicSource = true;
    [Tooltip("Music clip to play when musicTrigger is touched.")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Tooltip("If true, only plays music the first time this trigger is touched.")]
    public bool playMusicOnce = true;
    [Tooltip("Key that fades music out when pressed.")]
    public KeyCode fadeOutKey = KeyCode.E;
    public float fadeOutDuration = 2f;

    private bool hasEnabled;
    private bool hasPlayedMusic;
    private Coroutine fadeCoroutine;
    public static bool Act3DialogueGate = false;

    void Awake()
    {
        EnsureMusicSource();
    }

    void OnTriggerEnter(Collider other)
    {
        if (targetTrigger != null && other == targetTrigger)
            HandleEnableTrigger();

        if (musicTrigger != null && other == musicTrigger)
            HandleMusicTrigger();
    }

    void Update()
    {
        if (Input.GetKeyDown(fadeOutKey))
            FadeOutMusic();
    }

    void HandleEnableTrigger()
    {
        if (enableOnce && hasEnabled) return;
        if (objectToEnable == null) return;

        objectToEnable.SetActive(true);
        if (setAct3DialogueGate)
            Act3DialogueGate = true;
        hasEnabled = true;
    }

    void HandleMusicTrigger()
    {
        if (playMusicOnce && hasPlayedMusic) return;
        EnsureMusicSource();
        if (musicSource == null || musicClip == null) return;

        musicSource.clip = musicClip;
        musicSource.volume = musicVolume;
        musicSource.Play();
        hasPlayedMusic = true;
    }

    void EnsureMusicSource()
    {
        if (musicSource != null || !autoCreateMusicSource) return;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = false;
        musicSource.spatialBlend = 0f;
    }

    void FadeOutMusic()
    {
        if (musicSource == null || !musicSource.isPlaying) return;
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutMusicCoroutine());
    }

    IEnumerator FadeOutMusicCoroutine()
    {
        float duration = Mathf.Max(0.01f, fadeOutDuration);
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration && musicSource != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.volume = musicVolume;
        }

        fadeCoroutine = null;
    }
}
