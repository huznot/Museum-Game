using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class StreetLampPower : MonoBehaviour
{
    [Header("Global")]
    [Tooltip("Flip this to true from anywhere to restore power to all lamps.")]
    public static bool PowerOn
    {
        get => _powerOn;
        set
        {
            if (_powerOn == value) return;
            _powerOn = value;
            PowerChanged?.Invoke(_powerOn);
        }
    }

    public static event Action<bool> PowerChanged;
    static bool _powerOn = false;

    [Header("Timing")]
    [Tooltip("Random delay range before this lamp turns on after power is restored.")]
    public Vector2 startDelayRange = new Vector2(0.1f, 2.0f);

    [Header("Behavior")]
    [Tooltip("If true, also toggles each light GameObject on/off.")]
    public bool toggleLightGameObjects = true;
    [Tooltip("If true, enables child lights over multiple frames to reduce hitches.")]
    public bool gradualEnable = true;
    [Tooltip("How many lights to enable per frame when gradualEnable is true.")]
    public int lightsPerFrame = 4;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    Light[] childLights;
    System.Random rng;

    Coroutine turnOnRoutine;
    bool isOn;

    void Awake()
    {
        childLights = GetComponentsInChildren<Light>(true);
        rng = new System.Random(gameObject.GetInstanceID());

        if (childLights == null || childLights.Length == 0)
            Debug.LogWarning($"{name}: StreetLampPower found no child Light components.", this);

        SetLights(false);
    }

    void OnEnable()
    {
        PowerChanged += HandlePowerChanged;
        HandlePowerChanged(PowerOn);
    }

    void OnDisable()
    {
        PowerChanged -= HandlePowerChanged;
    }

    void HandlePowerChanged(bool on)
    {
        if (debugLogs)
            Debug.Log($"[StreetLampPower] {name} HandlePowerChanged({on})", this);

        if (on)
        {
            if (!isOn && turnOnRoutine == null)
                turnOnRoutine = StartCoroutine(TurnOnAfterDelay());
        }
        else
        {
            if (turnOnRoutine != null)
            {
                StopCoroutine(turnOnRoutine);
                turnOnRoutine = null;
            }

            if (isOn)
                SetLights(false);

            isOn = false;
        }
    }

    IEnumerator TurnOnAfterDelay()
    {
        float delay = Lerp(startDelayRange.x, startDelayRange.y, Next01());
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (gradualEnable && childLights != null && childLights.Length > 1)
        {
            int perFrame = Mathf.Max(1, lightsPerFrame);
            int enabledThisFrame = 0;
            for (int i = 0; i < childLights.Length; i++)
            {
                var lightComp = childLights[i];
                if (!lightComp) continue;

                if (toggleLightGameObjects && !lightComp.gameObject.activeSelf)
                    lightComp.gameObject.SetActive(true);

                lightComp.enabled = true;
                enabledThisFrame++;

                if (enabledThisFrame >= perFrame)
                {
                    enabledThisFrame = 0;
                    if (debugLogs)
                        Debug.Log($"[StreetLampPower] {name} enabled {i + 1}/{childLights.Length}", this);
                    yield return null;
                }
            }
        }
        else
        {
            SetLights(true);
        }
        isOn = true;
        turnOnRoutine = null;

        if (debugLogs)
            Debug.Log($"[StreetLampPower] {name} turn-on complete", this);
    }

    void SetLights(bool on)
    {
        if (childLights == null) return;

        for (int i = 0; i < childLights.Length; i++)
        {
            var lightComp = childLights[i];
            if (!lightComp) continue;

            if (on)
            {
                if (toggleLightGameObjects && !lightComp.gameObject.activeSelf)
                    lightComp.gameObject.SetActive(true);

                lightComp.enabled = true;
            }
            else
            {
                lightComp.enabled = false;

                if (toggleLightGameObjects && lightComp.gameObject.activeSelf)
                    lightComp.gameObject.SetActive(false);
            }
        }
    }

    float Next01() => (float)rng.NextDouble();
    float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);
}
