using UnityEngine;

public class NpcSpineIdleLook : MonoBehaviour
{
    [Header("Rotation Range (Local Euler)")]
    public Vector3 minEuler = new Vector3(6.95432711f, 23.5434704f, 3.01953554f);
    public Vector3 maxEuler = new Vector3(7.21954346f, 342.19754f, 357.688782f);

    [Header("Timing")]
    public Vector2 holdSeconds = new Vector2(1.0f, 3.0f);
    public Vector2 moveSeconds = new Vector2(1.0f, 2.8f);
    [Range(0f, 1f)] public float longHoldChance = 0.2f;
    public Vector2 longHoldSeconds = new Vector2(3.5f, 6.0f);

    [Header("Motion")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Range(0f, 1f)] public float microJitter = 0.02f;
    [Range(0f, 3f)] public float microSwayDegrees = 0.4f;
    public float microSwaySpeed = 0.28f;
    [Range(0f, 1f)] public float noMoveChance = 0.3f;

    [Header("Optional Blocking")]
    public MuseumNPCMover guideMover;
    public bool pauseWhileGuideIsWalking = true;

    private Quaternion startRot;
    private Quaternion targetRot;
    private float moveTime;
    private float moveElapsed;
    private float holdTime;
    private float holdElapsed;
    private bool moving;
    private Vector3 microSwayAxis;

    void Start()
    {
        microSwayAxis = Random.onUnitSphere;
        PickNewTarget(true);
    }

    void Update()
    {
        if (pauseWhileGuideIsWalking && guideMover != null && guideMover.IsWalking)
        {
            return;
        }

        if (moving)
        {
            moveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(moveElapsed / Mathf.Max(0.01f, moveTime));
            float eased = ease != null ? ease.Evaluate(t) : t;
            Quaternion blended = Quaternion.Slerp(startRot, targetRot, eased);

            if (microJitter > 0f)
            {
                float jitter = (Mathf.PerlinNoise(Time.time * 1.5f, 0.7f) - 0.5f) * microJitter;
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
            ApplyMicroSwayWhileHolding();
            if (holdElapsed >= holdTime)
                PickNewTarget(false);
        }
    }

    private void PickNewTarget(bool instant)
    {
        startRot = transform.localRotation;
        if (!instant && Random.value < noMoveChance)
        {
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

    private void ApplyMicroSwayWhileHolding()
    {
        if (microSwayDegrees <= 0f) return;
        float sway = Mathf.Sin(Time.time * microSwaySpeed) * microSwayDegrees;
        transform.localRotation = transform.localRotation * Quaternion.AngleAxis(sway * 0.02f, microSwayAxis);
    }
}
