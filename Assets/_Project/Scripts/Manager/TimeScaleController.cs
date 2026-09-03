using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Simple time controller: tap 1 to toggle slow-mo, 2 for 0.25x, 3 for 0.1x, tap P to pause/resume.
/// Adjusts fixedDeltaTime so physics stay in sync with the current timeScale.
/// </summary>
public class TimeScaleController : MonoBehaviour
{
    [SerializeField, Range(0.01f, 1f)] private float slowMotionScale = 0.5f;
    [SerializeField, Range(0.01f, 1f)] private float quarterSpeed = 0.25f;
    [SerializeField, Range(0.01f, 1f)] private float tenthSpeed = 0.1f;
    [SerializeField] private Key slowMotionKey = Key.Digit1;
    [SerializeField] private Key quarterSpeedKey = Key.Digit2;
    [SerializeField] private Key tenthSpeedKey = Key.Digit3;
    [SerializeField] private Key pauseKey = Key.P;
    public TMP_Text timeScaleText;

    private float _baseFixedDeltaTime;
    private bool _isPaused;
    private float _resumeScale = 1f;

    private void Awake()
    {
        _baseFixedDeltaTime = Time.fixedDeltaTime;
        SetTimeScale(1f);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard[slowMotionKey].wasPressedThisFrame)
        {
            if (_isPaused) return;
            // Toggle between the configured slow-mo scale and normal speed.
            float target = Mathf.Approximately(Time.timeScale, slowMotionScale) ? 1f : slowMotionScale;
            SetTimeScale(target);
        }

        if (keyboard[quarterSpeedKey].wasPressedThisFrame)
        {
            if (_isPaused) return;
            float target = Mathf.Approximately(Time.timeScale, quarterSpeed) ? 1f : quarterSpeed;
            SetTimeScale(target);
        }

        if (keyboard[tenthSpeedKey].wasPressedThisFrame)
        {
            if (_isPaused) return;
            float target = Mathf.Approximately(Time.timeScale, tenthSpeed) ? 1f : tenthSpeed;
            SetTimeScale(target);
        }

        if (keyboard[pauseKey].wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void OnDisable()
    {
        // Restore normal speed if this controller is turned off.
        SetTimeScale(1f);
        _isPaused = false;
    }

    private void SetTimeScale(float targetScale)
    {
        // Keep a non-zero fixedDeltaTime when paused to avoid odd physics state resets when resuming.
        if (targetScale <= 0f)
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = _baseFixedDeltaTime;
            UpdateTimeScaleLabel();
            return;
        }

        Time.timeScale = targetScale;
        Time.fixedDeltaTime = _baseFixedDeltaTime * targetScale;
        _resumeScale = targetScale;
        UpdateTimeScaleLabel();
    }

    private void TogglePause()
    {
        if (_isPaused)
        {
            _isPaused = false;
            SetTimeScale(_resumeScale <= 0f ? 1f : _resumeScale);
        }
        else
        {
            _isPaused = true;
            // Remember the current scale so we can restore it.
            _resumeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            SetTimeScale(0f);
        }
    }

    private void UpdateTimeScaleLabel()
    {
        if (timeScaleText == null)
            return;

        if (_isPaused || Time.timeScale <= 0f)
        {
            timeScaleText.text = "Paused";
            return;
        }

        if (Mathf.Approximately(Time.timeScale, 1f))
        {
            timeScaleText.text = "";
            return;
        }

        timeScaleText.text = $"{Time.timeScale:0.##}x speed";
    }
}
