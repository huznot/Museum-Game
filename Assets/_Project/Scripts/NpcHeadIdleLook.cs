using UnityEngine;

public class NpcHeadIdleLook : MonoBehaviour
{
    [Header("Rotation Range (Local Euler)")]
    public Vector3 minEuler = new Vector3(348.654877f, 326.639282f, 7.37915182f);
    public Vector3 maxEuler = new Vector3(354.75058f, 67.5103836f, 347.536896f);

    [Header("Timing")]
    [Tooltip("How long to stay still at each pose.")]
    public Vector2 holdSeconds = new Vector2(0.8f, 2.4f);
    [Tooltip("How long the turn takes.")]
    public Vector2 moveSeconds = new Vector2(0.9f, 2.2f);
    [Tooltip("Chance to pick a longer stillness hold.")]
    [Range(0f, 1f)] public float longHoldChance = 0.25f;
    [Tooltip("If long hold is chosen, this range overrides holdSeconds.")]
    public Vector2 longHoldSeconds = new Vector2(2.5f, 5.0f);

    [Header("Motion")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Small random wiggle. Keep low for natural feel.")]
    [Range(0f, 1f)] public float microJitter = 0.03f;
    [Tooltip("Slow breathing-like sway.")]
    [Range(0f, 3f)] public float microSwayDegrees = 0.6f;
    public float microSwaySpeed = 0.35f;
    [Tooltip("Chance to not move at all and just hold again.")]
    [Range(0f, 1f)] public float noMoveChance = 0.35f;
    [Tooltip("Occasional tiny saccade while holding.")]
    [Range(0f, 1f)] public float saccadeChance = 0.25f;
    public Vector2 saccadeDegrees = new Vector2(0.5f, 2.0f);
    public Vector2 saccadeHoldSeconds = new Vector2(0.08f, 0.25f);

    private Quaternion startRot;
    private Quaternion targetRot;
    private float moveTime;
    private float moveElapsed;
    private float holdTime;
    private float holdElapsed;
    private bool moving;
    private bool saccading;
    private Quaternion saccadeStart;
    private Quaternion saccadeTarget;
    private float saccadeTime;
    private float saccadeElapsed;
    private Vector3 microSwayAxis;

    void Start()
    {
        microSwayAxis = Random.onUnitSphere;
        PickNewTarget(true);
    }

    void Update()
    {
        if (saccading)
        {
            saccadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(saccadeElapsed / Mathf.Max(0.01f, saccadeTime));
            transform.localRotation = Quaternion.Slerp(saccadeStart, saccadeTarget, t);
            if (t >= 1f) saccading = false;
            return;
        }

        if (moving)
        {
            moveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(moveElapsed / Mathf.Max(0.01f, moveTime));
            float eased = ease != null ? ease.Evaluate(t) : t;
            Quaternion blended = Quaternion.Slerp(startRot, targetRot, eased);

            // tiny jitter for life
            if (microJitter > 0f)
            {
                float jitter = (Mathf.PerlinNoise(Time.time * 1.7f, 0.2f) - 0.5f) * microJitter;
                blended = blended * Quaternion.Euler(0f, jitter, 0f);
            }
            if (microSwayDegrees > 0f)
            {
                float sway = Mathf.Sin(Time.time * microSwaySpeed) * microSwayDegrees;
                blended = blended * Quaternion.AngleAxis(sway, microSwayAxis);
            }

            transform.localRotation = blended;

            if (t >= 1f)
            {
                moving = false;
                holdElapsed = 0f;
                holdTime = PickHoldTime();
            }
        }
        else
        {
            holdElapsed += Time.deltaTime;
            TrySaccade();
            ApplyMicroSwayWhileHolding();
            if (holdElapsed >= holdTime)
            {
                PickNewTarget(false);
            }
        }
    }

    private void PickNewTarget(bool instant)
    {
        startRot = transform.localRotation;
        if (!instant && Random.value < noMoveChance)
        {
            // Stay still, just extend the hold.
            moving = false;
            holdElapsed = 0f;
            holdTime = PickHoldTime();
            return;
        }
        Vector3 euler = new Vector3(
            RandomRangeAngle(minEuler.x, maxEuler.x),
            RandomRangeAngle(minEuler.y, maxEuler.y),
            RandomRangeAngle(minEuler.z, maxEuler.z)
        );
        targetRot = Quaternion.Euler(euler);
        moveTime = Random.Range(moveSeconds.x, moveSeconds.y);
        moveElapsed = 0f;
        moving = true;

        if (instant)
        {
            transform.localRotation = targetRot;
            moving = false;
            holdElapsed = 0f;
            holdTime = PickHoldTime();
        }
    }

    private float RandomRangeAngle(float a, float b)
    {
        return Mathf.LerpAngle(a, b, Random.value);
    }

    private float PickHoldTime()
    {
        if (Random.value < longHoldChance)
            return Random.Range(longHoldSeconds.x, longHoldSeconds.y);
        return Random.Range(holdSeconds.x, holdSeconds.y);
    }

    private void TrySaccade()
    {
        if (saccading) return;
        if (Random.value > saccadeChance * Time.deltaTime) return;

        float deg = Random.Range(saccadeDegrees.x, saccadeDegrees.y);
        Vector3 axis = Random.onUnitSphere;
        saccadeStart = transform.localRotation;
        saccadeTarget = saccadeStart * Quaternion.AngleAxis(deg, axis);
        saccadeTime = Random.Range(saccadeHoldSeconds.x, saccadeHoldSeconds.y);
        saccadeElapsed = 0f;
        saccading = true;
    }

    private void ApplyMicroSwayWhileHolding()
    {
        if (microSwayDegrees <= 0f) return;
        float sway = Mathf.Sin(Time.time * microSwaySpeed) * microSwayDegrees;
        transform.localRotation = transform.localRotation * Quaternion.AngleAxis(sway * 0.02f, microSwayAxis);
    }
}
