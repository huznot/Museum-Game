using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Dedicated IK-driven fuel cap interaction (no dependency on ArmMouseIKRig2).
/// Press E within range to reach, pull back, and rotate the cap open/closed.
/// </summary>
public class FuelCapInteractable : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Object that rotates to open the fuel cap (can be the same as this object).")]
    public Transform fuelCap;
    [Tooltip("Point the hand should reach on the cap.")]
    public Transform fuelCapTarget;
    [Tooltip("TwoBoneIKConstraint for the left arm dedicated to this interaction.")]
    public TwoBoneIKConstraint handIK;
    [Tooltip("IK target Transform for the above constraint.")]
    public Transform handTarget;
    [Tooltip("Optional rest pose target; if null, current handTarget pose is used as rest.")]
    public Transform handRestPose;
    [Tooltip("Player root or camera used to measure interaction distance.")]
    public Transform playerReference;
    [Tooltip("Camera used to test if the center of the screen points at the fuel cap (falls back to main camera).")]
    public Camera playerCamera;
    [Tooltip("Sprite/UI prompt shown when in range and aiming at the cap.")]
    public GameObject aimPrompt;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 1.0f;
    public float reachDuration = 0.25f;
    public float openDuration = 0.6f;
    public float openAngle = 110f;
    public Vector3 rotationAxis = Vector3.forward; // Z-axis as requested
    [Tooltip("Local offset (relative to fuelCapTarget) applied while opening to look like a slight pull back.")]
    public Vector3 pullBackLocalOffset = new Vector3(0f, 0f, -0.06f);
    [Tooltip("Default local offset for the palm on the cap target (helps avoid sideways drift).")]
    public Vector3 handLocalOffset = Vector3.zero;
    [Tooltip("Local rotation (Euler) for the palm on the cap target.")]
    public Vector3 handLocalRotation = Vector3.zero;

    [Header("Audio")]
    public AudioClip fuelCapOpenClip;
    public AudioClip fuelCapCloseClip;
    public AudioSource fuelCapAudioSource;
    [Range(0f, 1f)] public float fuelCapVolume = 0.9f;
    public float fuelCapPitch = 1f;

    [Header("IK Weighting")]
    public float ikWeightUpSpeed = 8f;
    public float ikWeightDownSpeed = 6f;

    [Header("Debug")]
    public bool debugLogs = false;

    private bool isAnimating;
    private bool isOpen;
    private Quaternion initialRotation;
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Vector3 restPosition;
    private Quaternion restRotation;

    void Awake()
    {
        if (fuelCap == null)
            fuelCap = transform;

        initialRotation = fuelCap.localRotation;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (aimPrompt != null)
            aimPrompt.SetActive(false);

        EnsureAudioSource();
    }

    void Start()
    {
        if (handTarget != null)
        {
            originalParent = handTarget.parent;
            originalLocalPos = handTarget.localPosition;
            originalLocalRot = handTarget.localRotation;
            restPosition = handRestPose ? handRestPose.position : handTarget.position;
            restRotation = handRestPose ? handRestPose.rotation : handTarget.rotation;
        }
    }

    void Update()
    {
        UpdatePrompt();

        if (isAnimating || isOpen)
        {
            if (Input.GetKeyDown(interactKey) && IsLookingAtCap())
            {
                StartCoroutine(CloseFuelCapRoutine());
            }
            return;
        }

        if (Input.GetKeyDown(interactKey) && IsInRange() && IsLookingAtCap())
        {
            if (fuelCapTarget == null || handIK == null || handTarget == null)
            {
                Debug.LogWarning("Fuel cap interaction missing references.");
                return;
            }

            StartCoroutine(OpenFuelCapRoutine());
        }
    }

    bool IsInRange()
    {
        if (playerReference == null || fuelCapTarget == null)
            return false;

        return Vector3.Distance(playerReference.position, fuelCapTarget.position) <= interactDistance;
    }

    bool IsLookingAtCap()
    {
        if (playerCamera == null || fuelCapTarget == null)
            return false;

        Vector3 toCap = (fuelCapTarget.position - playerCamera.transform.position).normalized;
        float dot = Vector3.Dot(playerCamera.transform.forward, toCap);
        // 0.98 ~ within ~11 degrees of the center dot
        return dot >= 0.98f;
    }

    void UpdatePrompt()
    {
        if (aimPrompt == null)
            return;

        bool show = !isAnimating && IsInRange() && IsLookingAtCap();
        if (aimPrompt.activeSelf != show)
            aimPrompt.SetActive(show);
    }

    IEnumerator OpenFuelCapRoutine()
    {
        isAnimating = true;

        PlayFuelCapSound(fuelCapOpenClip);

        float originalWeight = handIK.weight;
        yield return StartCoroutine(SetIKWeight(1f, ikWeightUpSpeed));

        Quaternion startHandRot = handTarget.rotation;
        Vector3 startHandPos = handTarget.position;
        Quaternion startCapRot = fuelCap.localRotation;
        Quaternion targetCapRot = initialRotation * Quaternion.AngleAxis(openAngle, rotationAxis);
        Vector3 pullBackWorld = fuelCapTarget.TransformPoint(pullBackLocalOffset);

        if (debugLogs)
        {
            Debug.Log($"[FuelCap] OPEN start | handTarget={handTarget.name} pos={startHandPos} rot={startHandRot.eulerAngles} | capTargetPos={fuelCapTarget.position} capTargetRot={fuelCapTarget.rotation.eulerAngles} | pullBackWorld={pullBackWorld}");
        }

        // Phase 1: reach to the cap target.
        float elapsed = 0f;
        while (elapsed < reachDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / reachDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            handTarget.position = Vector3.Lerp(startHandPos, fuelCapTarget.TransformPoint(handLocalOffset), t);
            handTarget.rotation = Quaternion.Slerp(startHandRot, fuelCapTarget.rotation * Quaternion.Euler(handLocalRotation), t);
            yield return null;
        }
        handTarget.position = fuelCapTarget.TransformPoint(handLocalOffset);
        handTarget.rotation = fuelCapTarget.rotation * Quaternion.Euler(handLocalRotation);
        handTarget.SetParent(fuelCapTarget, true);

        // Phase 2: pull and open.
        elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            fuelCap.localRotation = Quaternion.Slerp(startCapRot, targetCapRot, t);
            handTarget.position = Vector3.Lerp(fuelCapTarget.TransformPoint(handLocalOffset), pullBackWorld, t);
            handTarget.rotation = Quaternion.Slerp(fuelCapTarget.rotation * Quaternion.Euler(handLocalRotation), startHandRot, 0.35f * t); // slight ease back
            yield return null;
        }

        fuelCap.localRotation = targetCapRot;
        handTarget.position = pullBackWorld;
        handTarget.rotation = Quaternion.Slerp(fuelCapTarget.rotation * Quaternion.Euler(handLocalRotation), startHandRot, 0.35f);
        handTarget.SetParent(originalParent, true);
        handTarget.localPosition = originalLocalPos;
        handTarget.localRotation = originalLocalRot;

        yield return StartCoroutine(SetIKWeight(originalWeight, ikWeightDownSpeed));

        isOpen = true;
        isAnimating = false;

        if (debugLogs)
        {
            Debug.Log($"[FuelCap] OPEN done | handTarget pos={handTarget.position} rot={handTarget.rotation.eulerAngles} | capRot={fuelCap.localRotation.eulerAngles}");
        }
    }

    IEnumerator CloseFuelCapRoutine()
    {
        isAnimating = true;

        PlayFuelCapSound(fuelCapCloseClip);

        float originalWeight = handIK.weight;
        yield return StartCoroutine(SetIKWeight(1f, ikWeightUpSpeed));

        Vector3 startHandPos = handTarget.position;
        Quaternion startHandRot = handTarget.rotation;
        originalParent = handTarget.parent;
        originalLocalPos = handTarget.localPosition;
        originalLocalRot = handTarget.localRotation;

        Quaternion startCapRot = fuelCap.localRotation;
        Quaternion targetCapRot = initialRotation;
        Vector3 pullBackWorld = fuelCapTarget.TransformPoint(pullBackLocalOffset);

        if (debugLogs)
        {
            Debug.Log($"[FuelCap] CLOSE start | handTarget={handTarget.name} pos={startHandPos} rot={startHandRot.eulerAngles} | capTargetPos={fuelCapTarget.position} capTargetRot={fuelCapTarget.rotation.eulerAngles} | pullBackWorld={pullBackWorld}");
        }

        // Phase 1: move from current (likely pull-back) to the cap target
        float elapsed = 0f;
        while (elapsed < reachDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / reachDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            handTarget.position = Vector3.Lerp(startHandPos, fuelCapTarget.TransformPoint(handLocalOffset), t);
            handTarget.rotation = Quaternion.Slerp(startHandRot, fuelCapTarget.rotation * Quaternion.Euler(handLocalRotation), t);
            yield return null;
        }
        handTarget.position = fuelCapTarget.TransformPoint(handLocalOffset);
        handTarget.rotation = fuelCapTarget.rotation * Quaternion.Euler(handLocalRotation);
        handTarget.SetParent(fuelCapTarget, true);

        // Phase 2: rotate cap closed with the same pull-back motion for consistency
        elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            fuelCap.localRotation = Quaternion.Slerp(startCapRot, targetCapRot, t);
            handTarget.position = Vector3.Lerp(fuelCapTarget.TransformPoint(handLocalOffset), pullBackWorld, t);
            handTarget.rotation = Quaternion.Slerp(fuelCapTarget.rotation * Quaternion.Euler(handLocalRotation), startHandRot, 0.35f * t);
            yield return null;
        }

        fuelCap.localRotation = targetCapRot;
        handTarget.position = pullBackWorld;
        handTarget.rotation = Quaternion.Slerp(fuelCapTarget.rotation * Quaternion.Euler(handLocalRotation), startHandRot, 0.35f);
        handTarget.SetParent(originalParent, true);
        handTarget.localPosition = originalLocalPos;
        handTarget.localRotation = originalLocalRot;

        yield return StartCoroutine(SetIKWeight(originalWeight, ikWeightDownSpeed));

        isOpen = false;
        isAnimating = false;

        if (debugLogs)
        {
            Debug.Log($"[FuelCap] CLOSE done | handTarget pos={handTarget.position} rot={handTarget.rotation.eulerAngles} | capRot={fuelCap.localRotation.eulerAngles}");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (fuelCapTarget == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(fuelCapTarget.position, interactDistance);
    }

    IEnumerator SetIKWeight(float targetWeight, float speed)
    {
        if (handIK == null) yield break;
        float start = handIK.weight;
        float elapsed = 0f;
        float duration = Mathf.Approximately(speed, 0f) ? 0f : Mathf.Abs(targetWeight - start) / speed;
        duration = Mathf.Max(0.0001f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            handIK.weight = Mathf.Lerp(start, targetWeight, t);
            yield return null;
        }
        handIK.weight = targetWeight;
    }

    void EnsureAudioSource()
    {
        if (fuelCapAudioSource != null) return;
        fuelCapAudioSource = gameObject.AddComponent<AudioSource>();
        fuelCapAudioSource.playOnAwake = false;
        fuelCapAudioSource.loop = false;
        fuelCapAudioSource.spatialBlend = 1f;
    }

    void PlayFuelCapSound(AudioClip clip)
    {
        if (clip == null || fuelCapAudioSource == null) return;
        fuelCapAudioSource.pitch = fuelCapPitch;
        fuelCapAudioSource.volume = fuelCapVolume;
        fuelCapAudioSource.PlayOneShot(clip);
    }
}
