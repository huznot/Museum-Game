using UnityEngine;

public class SunRotationLerp : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform sun;
    [SerializeField] private Transform sunAmbient;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float durationSeconds = 300f;
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField] private bool playOnEnable = true;

    [Header("Rotations (Euler)")]
    [SerializeField] private Vector3 sunStartEuler = new Vector3(14.9017038f, 345.303223f, 326.731476f);
    [SerializeField] private Vector3 sunTargetEuler = new Vector3(3.15844631f, 328.239807f, 324.019653f);
    [SerializeField] private Vector3 ambientStartEuler = new Vector3(0.875033319f, 144.400162f, 323.909149f);
    [SerializeField] private Vector3 ambientTargetEuler = new Vector3(348.925232f, 127.629776f, 325.418762f);

    [Header("Start Behavior")]
    [SerializeField] private bool useCurrentAsStart = false;
    [SerializeField] private bool setToStartOnBegin = true;

    [Header("Fog Color (Final Phase)")]
    [SerializeField] private bool lerpFogColor = true;
    [SerializeField, Range(0f, 1f)] private float fogLerpStartNormalized = 0.9f;
    [SerializeField] private Color fogTargetColor = new Color32(0x8F, 0x8F, 0x8F, 0xFF);

    private float _elapsed;
    private bool _running;
    private Quaternion _sunStartRot;
    private Quaternion _sunTargetRot;
    private Quaternion _ambientStartRot;
    private Quaternion _ambientTargetRot;
    private Color _fogStartColor;

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Begin();
        }
    }

    public void Begin()
    {
        if (sun == null && sunAmbient == null)
            return;

        if (useCurrentAsStart)
        {
            if (sun != null)
                sunStartEuler = sun.eulerAngles;
            if (sunAmbient != null)
                ambientStartEuler = sunAmbient.eulerAngles;
        }

        _sunStartRot = Quaternion.Euler(sunStartEuler);
        _sunTargetRot = Quaternion.Euler(sunTargetEuler);
        _ambientStartRot = Quaternion.Euler(ambientStartEuler);
        _ambientTargetRot = Quaternion.Euler(ambientTargetEuler);

        if (setToStartOnBegin)
        {
            if (sun != null)
                sun.rotation = _sunStartRot;
            if (sunAmbient != null)
                sunAmbient.rotation = _ambientStartRot;
        }

        _fogStartColor = RenderSettings.fogColor;

        _elapsed = 0f;
        _running = true;
    }

    private void Update()
    {
        if (!_running)
            return;

        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _elapsed += delta;

        float t = durationSeconds <= 0f ? 1f : Mathf.Clamp01(_elapsed / durationSeconds);

        if (sun != null)
            sun.rotation = Quaternion.Slerp(_sunStartRot, _sunTargetRot, t);
        if (sunAmbient != null)
            sunAmbient.rotation = Quaternion.Slerp(_ambientStartRot, _ambientTargetRot, t);

        if (lerpFogColor && t >= fogLerpStartNormalized)
        {
            float denom = 1f - fogLerpStartNormalized;
            float fogT = denom <= 0f ? 1f : Mathf.Clamp01((t - fogLerpStartNormalized) / denom);
            RenderSettings.fogColor = Color.Lerp(_fogStartColor, fogTargetColor, fogT);
        }

        if (t >= 1f)
            _running = false;
    }
}
