using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityTutorial.PlayerControl;
using MicrophoneInput;

public class MuseumQuizUI : MonoBehaviour
{
    public static MuseumQuizUI Instance;

    [Header("UI References")]
    [Tooltip("CanvasGroup on the root Prompt GameObject")]
    public CanvasGroup promptGroup;
    public TMP_Text questionText;
    public TMP_Text option1Text;
    public TMP_Text option2Text;
    public TMP_Text option3Text;
    [Tooltip("The back/highlight RawImage for option 1 (1back)")]
    public RawImage back1Image;
    [Tooltip("The back/highlight RawImage for option 2 (2back)")]
    public RawImage back2Image;
    [Tooltip("The back/highlight RawImage for option 3 (3back)")]
    public RawImage back3Image;

    [Header("Timing")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.6f;
    [Tooltip("How long the flash takes to go invisible and return")]
    public float flashDuration = 0.12f;

    private Action<bool> _onAnswered;
    private int _correctIndex;
    private bool _waitingForInput;
    private float[] _backOriginalAlphas = new float[3];

    private void Awake()
    {
        Instance = this;
        HideImmediate();
    }

    // Called by Awake and also directly if the object starts inactive
    private void HideImmediate()
    {
        if (promptGroup != null)
        {
            promptGroup.alpha = 0f;
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        // Make sure Instance is set even if this object was inactive at scene start
        if (Instance == null) Instance = this;
        HideImmediate();
    }

    public void ShowQuiz(MuseumMicrophone.QuizData quiz, Action<bool> onAnswered)
    {
        _onAnswered = onAnswered;
        _correctIndex = quiz.correctOptionIndex;

        if (questionText != null) questionText.text = quiz.questionText;
        if (option1Text != null) option1Text.text = quiz.option1;
        if (option2Text != null) option2Text.text = quiz.option2;
        if (option3Text != null) option3Text.text = quiz.option3;

        // Reset all back images to invisible so the next quiz always starts clean
        SetImageAlpha(back1Image, 0f);
        SetImageAlpha(back2Image, 0f);
        SetImageAlpha(back3Image, 0f);

        SetPlayerUIMode(true);
        StartCoroutine(QuizRoutine());
    }

    private IEnumerator QuizRoutine()
    {
        yield return StartCoroutine(FadePrompt(0f, 1f, fadeInDuration));
        promptGroup.interactable = true;
        promptGroup.blocksRaycasts = true;
        _waitingForInput = true;
    }

    private void Update()
    {
        if (!_waitingForInput) return;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            StartCoroutine(SelectOption(0));
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            StartCoroutine(SelectOption(1));
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            StartCoroutine(SelectOption(2));
    }

    private IEnumerator SelectOption(int index)
    {
        _waitingForInput = false;
        promptGroup.interactable = false;

        // Flash: snap to 0, quickly fade up to full opacity, then the whole prompt fades out
        RawImage backImg = GetBackImage(index);
        if (backImg != null)
        {
            yield return StartCoroutine(FadeImage(backImg, 0f, 1f, flashDuration));
        }

        // Fade out whole prompt
        yield return StartCoroutine(FadePrompt(1f, 0f, fadeOutDuration));
        promptGroup.blocksRaycasts = false;

        SetPlayerUIMode(false);
        _onAnswered?.Invoke(index == _correctIndex);
    }

    private RawImage GetBackImage(int index)
    {
        if (index == 0) return back1Image;
        if (index == 1) return back2Image;
        if (index == 2) return back3Image;
        return null;
    }

    private IEnumerator FadePrompt(float from, float to, float duration)
    {
        if (promptGroup == null) yield break;
        float elapsed = 0f;
        promptGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            promptGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        promptGroup.alpha = to;
    }

    private void SetImageAlpha(RawImage img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    private IEnumerator FadeImage(RawImage img, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = img.color;
        c.a = fromAlpha;
        img.color = c;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            img.color = c;
            yield return null;
        }
        c.a = toAlpha;
        img.color = c;
    }

    private void SetPlayerUIMode(bool value)
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.uiMode = value;

        var fpc = FindFirstObjectByType<FlashbackPlayerController>();
        if (fpc != null) fpc.uiMode = value;
    }
}
