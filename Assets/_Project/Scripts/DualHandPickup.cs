using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using TMPro;

/// <summary>
/// Dual-hand pickup: on E, move the item to the hold empty and move both IK targets to their grab points smoothly.
/// </summary>
public class DualHandPickup : MonoBehaviour
{
    [Header("References")]
    public Transform itemRoot;            // object to move (defaults to this)
    public Transform holdPose;            // empty on the player (world pose to place the item)
    public TwoBoneIKConstraint leftIK;
    public TwoBoneIKConstraint rightIK;
    public Transform leftHandTarget;      // target objects used by the IK constraints
    public Transform rightHandTarget;
    public Transform leftGrabPoint;       // points on/near the item
    public Transform rightGrabPoint;
    public Transform playerReference;     // for distance checks
    public Camera playerCamera;           // for aim checks (defaults to main)
    public GameObject aimPrompt;          // UI shown when in range and looking

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 1.5f;
    [Range(0.9f, 1f)] public float aimDotThreshold = 0.98f; // how centered you must be
    public float moveDuration = 0.6f;     // time to move item to hold pose
    public float reachSmooth = 10f;       // higher = hands move snappier to grab points
    public float ikWeightSpeed = 10f;     // higher = faster IK weight ramp
    public bool parentAfterPickup = true; // parent to holdPose after moving
    public float settleDuration = 0.25f;  // optional final settle to remove tiny gaps
    public float settleSmoothing = 12f;
    public Transform putBackTarget;       // object to look at to put the item back
    public Transform pourTarget;          // object to look at to pour
    public GameObject pourPrompt;         // UI shown when can pour
    public float pourAngle = 25f;         // forward tilt when pouring
    public float pourSpeed = 8f;          // smoothing speed for pour tilt
    public Vector3 pourTiltAxisLocal = Vector3.right; // axis in local space to tilt around
    public float secondaryInteractDistance = 2.5f;    // range for put back / pour targets
    public TMP_Text refuelText;           // text UI to show refuel progress
    public CanvasGroup refuelCanvasGroup; // optional canvas group for fading
    public float refuelDuration = 30f;    // seconds to reach 100%
    public float refuelFadeDuration = 1f; // fade duration in seconds

    [Header("Hand Tweaks")]
    public bool lockRightHandRoll = true; // keep right wrist from twisting with grab point roll
    public Vector3 rightHandUpHint = Vector3.up;

    [Header("Debug")]
    public bool debugLogs = false;

    bool isAnimating;
    bool isHeld;
    float leftDefaultWeight;
    float rightDefaultWeight;
    Vector3 originalPosition;
    Quaternion originalRotation;
    Vector3 originalLocalPosition;
    Quaternion originalLocalRotation;
    Transform originalParent;
    Quaternion heldRestLocalRotation;
    float pourState;
    float refuelProgress;
    Coroutine refuelFadeRoutine;

    void Awake()
    {
        if (itemRoot == null)
            itemRoot = transform;
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Cache original pose to restore on put-back
        originalParent = itemRoot.parent;
        originalPosition = itemRoot.position;
        originalRotation = itemRoot.rotation;
        originalLocalPosition = itemRoot.localPosition;
        originalLocalRotation = itemRoot.localRotation;
        heldRestLocalRotation = itemRoot.parent != null ? itemRoot.localRotation : itemRoot.rotation;

        EnsureRefuelCanvasGroup();
        SetRefuelVisible(false, true);
    }

    void Start()
    {
        if (leftIK != null) leftDefaultWeight = leftIK.weight;
        if (rightIK != null) rightDefaultWeight = rightIK.weight;
        if (aimPrompt != null) aimPrompt.SetActive(false);
    }

    void Update()
    {
        UpdatePrompt();

        if (isAnimating)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            if (!isHeld && IsInRange() && IsLookingAtItem())
            {
                if (!ValidateReferences()) return;
                StartCoroutine(PickupRoutine());
            }
            else if (isHeld && CanPutBack())
            {
                StartCoroutine(PutBackRoutine());
            }
        }

        UpdatePour();
    }

    bool ValidateReferences()
    {
        if (itemRoot == null || holdPose == null || leftIK == null || rightIK == null ||
            leftHandTarget == null || rightHandTarget == null ||
            leftGrabPoint == null || rightGrabPoint == null)
        {
            Debug.LogWarning("DualHandPickup: missing references.");
            return false;
        }
        return true;
    }

    bool IsInRange()
    {
        if (playerReference == null || itemRoot == null) return false;
        return Vector3.Distance(playerReference.position, itemRoot.position) <= interactDistance;
    }

    bool IsInRangeTarget(Transform target)
    {
        if (playerReference == null || target == null) return false;
        return Vector3.Distance(playerReference.position, target.position) <= secondaryInteractDistance;
    }

    bool IsLookingAtItem()
    {
        return IsLookingAtTarget(itemRoot);
    }

    bool IsLookingAtTarget(Transform target)
    {
        if (playerCamera == null || target == null) return false;
        Vector3 toItem = (target.position - playerCamera.transform.position).normalized;
        float dot = Vector3.Dot(playerCamera.transform.forward, toItem);
        return dot >= aimDotThreshold;
    }

    bool CanPutBack()
    {
        if (!isHeld || putBackTarget == null) return false;
        return IsInRangeTarget(putBackTarget) && IsLookingAtTarget(putBackTarget);
    }

    void UpdatePrompt()
    {
        if (aimPrompt == null) return;
        bool show = !isAnimating && (
            (!isHeld && IsInRange() && IsLookingAtItem()) ||
            (isHeld && CanPutBack()));
        if (aimPrompt.activeSelf != show)
            aimPrompt.SetActive(show);

        if (pourPrompt != null)
        {
            bool pourShow = !isAnimating && isHeld && CanPour();
            if (pourPrompt.activeSelf != pourShow)
                pourPrompt.SetActive(pourShow);
        }
    }

    IEnumerator PickupRoutine()
    {
        isAnimating = true;
        if (aimPrompt != null) aimPrompt.SetActive(false);

        Vector3 startPos = itemRoot.position;
        Quaternion startRot = itemRoot.rotation;
        Vector3 targetPos = holdPose.position;
        Quaternion targetRot = holdPose.rotation;
        // Desired world rotation with X = -90
        Vector3 targetEuler = targetRot.eulerAngles;
        targetEuler.x = -90f;
        targetEuler.z = 270f;
        targetRot = Quaternion.Euler(targetEuler);

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            // Move item toward hold pose
            itemRoot.position = Vector3.Lerp(startPos, targetPos, t);
            itemRoot.rotation = Quaternion.Slerp(startRot, targetRot, t);

            // Move IK weights up
            float wLerp = 1f - Mathf.Exp(-ikWeightSpeed * Time.deltaTime);
            leftIK.weight = Mathf.Lerp(leftIK.weight, 1f, wLerp);
            rightIK.weight = Mathf.Lerp(rightIK.weight, 1f, wLerp);

            // Move hand targets toward grab points (which move with the item)
            float handLerp = 1f - Mathf.Exp(-reachSmooth * Time.deltaTime);
            leftHandTarget.position = Vector3.Lerp(leftHandTarget.position, leftGrabPoint.position, handLerp);
            leftHandTarget.rotation = Quaternion.Slerp(leftHandTarget.rotation, leftGrabPoint.rotation, handLerp);
            rightHandTarget.position = Vector3.Lerp(rightHandTarget.position, rightGrabPoint.position, handLerp);
            Quaternion rightTargetRot = GetRightHandRotation();
            rightHandTarget.rotation = Quaternion.Slerp(rightHandTarget.rotation, rightTargetRot, handLerp);

            yield return null;
        }

        // Snap to final
        itemRoot.position = targetPos;
        itemRoot.rotation = targetRot;
        leftIK.weight = 1f;
        rightIK.weight = 1f;
        leftHandTarget.position = leftGrabPoint.position;
        leftHandTarget.rotation = leftGrabPoint.rotation;
        rightHandTarget.position = rightGrabPoint.position;
        rightHandTarget.rotation = GetRightHandRotation();

        if (parentAfterPickup)
        {
            itemRoot.SetParent(holdPose, true);
            // Re-apply world rotation after parenting to maintain -90 on X and exact position
            // Vector3 finalEuler = itemRoot.rotation.eulerAngles;
            // finalEuler.x = -90f;
            // finalEuler.y = -90f;
            // itemRoot.rotation = Quaternion.Euler(finalEuler);
            // itemRoot.position = holdPose.position;
        }

        // Optional settle toward hand midpoint to eliminate any remaining gap
        if (leftGrabPoint != null && rightGrabPoint != null && settleDuration > 0f)
        {
            float settleElapsed = 0f;
            while (settleElapsed < settleDuration)
            {
                settleElapsed += Time.deltaTime;
                Vector3 grabMid = 0.5f * (leftGrabPoint.position + rightGrabPoint.position);
                Vector3 handMid = 0.5f * (leftHandTarget.position + rightHandTarget.position);
                Vector3 delta = handMid - grabMid;

                float lerp = 1f - Mathf.Exp(-settleSmoothing * Time.deltaTime);
                itemRoot.position += delta * lerp;

                if (delta.magnitude <= 0.001f)
                    break;

                yield return null;
            }
        }

        isHeld = true;
        isAnimating = false;
        heldRestLocalRotation = itemRoot.parent != null ? itemRoot.localRotation : itemRoot.rotation;
        pourState = 0f;
        SetRefuelVisible(false, true);

        if (debugLogs)
            Debug.Log("[DualHandPickup] Pickup complete.");
    }

    IEnumerator PutBackRoutine()
    {
        isAnimating = true;
        if (aimPrompt != null) aimPrompt.SetActive(false);

        if (parentAfterPickup && itemRoot.parent == holdPose)
            itemRoot.SetParent(originalParent, true);

        Vector3 startPos = itemRoot.position;
        Quaternion startRot = itemRoot.rotation;
        Vector3 targetPos = originalParent != null ? originalParent.TransformPoint(originalLocalPosition) : originalPosition;
        Quaternion targetRot = originalParent != null ? originalParent.rotation * originalLocalRotation : originalRotation;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            itemRoot.position = Vector3.Lerp(startPos, targetPos, t);
            itemRoot.rotation = Quaternion.Slerp(startRot, targetRot, t);

            float wLerp = 1f - Mathf.Exp(-ikWeightSpeed * Time.deltaTime);
            leftIK.weight = Mathf.Lerp(leftIK.weight, leftDefaultWeight, wLerp);
            rightIK.weight = Mathf.Lerp(rightIK.weight, rightDefaultWeight, wLerp);

            yield return null;
        }

        itemRoot.position = targetPos;
        itemRoot.rotation = targetRot;
        if (parentAfterPickup)
        {
            itemRoot.SetParent(originalParent, false);
            if (originalParent != null)
            {
                itemRoot.localPosition = originalLocalPosition;
                itemRoot.localRotation = originalLocalRotation;
            }
        }

        leftIK.weight = leftDefaultWeight;
        rightIK.weight = rightDefaultWeight;

        isHeld = false;
        isAnimating = false;
        pourState = 0f;
        SetRefuelVisible(false, true);

        if (debugLogs)
            Debug.Log("[DualHandPickup] Put back complete.");
    }

    void UpdatePour()
    {
        bool canPour = CanPour();
        bool wantsPour = canPour && Input.GetMouseButton(0);

        if (!isHeld || isAnimating || pourTarget == null)
        {
            pourState = 0f;
            UpdateRefuelUI(false, canPour);
            return;
        }

        float target = wantsPour ? 1f : 0f;
        float lerp = 1f - Mathf.Exp(-pourSpeed * Time.deltaTime);
        pourState = Mathf.Lerp(pourState, target, lerp);

        if (wantsPour && refuelDuration > 0f)
        {
            refuelProgress = Mathf.Clamp01(refuelProgress + Time.deltaTime / refuelDuration);
        }

        if (refuelProgress >= 1f)
        {
            CarController.triggerFuelDrop = true;
        }

        Quaternion baseRot = GetHeldRestRotationWorld();
        Vector3 axisWorld = baseRot * pourTiltAxisLocal.normalized;
        Quaternion tiltRot = Quaternion.AngleAxis(pourAngle, axisWorld) * baseRot;
        itemRoot.rotation = Quaternion.Slerp(baseRot, tiltRot, pourState);

        UpdateRefuelUI(wantsPour, canPour);
    }

    Quaternion GetRightHandRotation()
    {
        if (!lockRightHandRoll)
            return rightGrabPoint.rotation;

        Vector3 up = rightHandUpHint.sqrMagnitude > 0.0001f ? rightHandUpHint.normalized : Vector3.up;
        return Quaternion.LookRotation(rightGrabPoint.forward, up);
    }

    bool CanPour()
    {
        return isHeld && pourTarget != null && IsInRangeTarget(pourTarget) && IsLookingAtTarget(pourTarget);
    }

    Quaternion GetHeldRestRotationWorld()
    {
        if (itemRoot.parent != null)
            return itemRoot.parent.rotation * heldRestLocalRotation;
        return heldRestLocalRotation;
    }

    void UpdateRefuelUI(bool wantsPour, bool canPour)
    {
        if (refuelText == null) return;

        int percent = Mathf.RoundToInt(refuelProgress * 100f);
        refuelText.text = $"Refueling: {percent}%";

        bool visible = wantsPour && canPour;
        SetRefuelVisible(visible, false);
    }

    void EnsureRefuelCanvasGroup()
    {
        if (refuelCanvasGroup != null) return;
        if (refuelText != null)
        {
            refuelCanvasGroup = refuelText.GetComponentInParent<CanvasGroup>();
            if (refuelCanvasGroup == null)
                refuelCanvasGroup = refuelText.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void SetRefuelVisible(bool show, bool instant)
    {
        if (refuelText == null) return;
        EnsureRefuelCanvasGroup();

        if (refuelFadeRoutine != null)
        {
            StopCoroutine(refuelFadeRoutine);
            refuelFadeRoutine = null;
        }

        if (refuelCanvasGroup == null || instant || refuelFadeDuration <= 0f)
        {
            float alpha = show ? 1f : 0f;
            ApplyRefuelAlpha(alpha);
            if (!show && refuelText.gameObject.activeSelf)
                refuelText.gameObject.SetActive(false);
            else if (show && !refuelText.gameObject.activeSelf)
                refuelText.gameObject.SetActive(true);
        }
        else
        {
            // Ensure active so it can fade in
            if (show && !refuelText.gameObject.activeSelf)
                refuelText.gameObject.SetActive(true);
            refuelFadeRoutine = StartCoroutine(FadeCanvasGroup(refuelCanvasGroup, refuelCanvasGroup.alpha, show ? 1f : 0f, refuelFadeDuration, deactivateOnComplete: !show));
        }
    }

    void ApplyRefuelAlpha(float alpha)
    {
        if (refuelCanvasGroup != null)
            refuelCanvasGroup.alpha = alpha;
        else
        {
            Color c = refuelText.color;
            c.a = alpha;
            refuelText.color = c;
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration, bool deactivateOnComplete)
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = from;
        duration = Mathf.Max(0f, duration);
        if (duration == 0f)
        {
            canvasGroup.alpha = to;
            if (deactivateOnComplete && Mathf.Approximately(to, 0f) && refuelText != null)
                refuelText.gameObject.SetActive(false);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
        if (deactivateOnComplete && Mathf.Approximately(to, 0f) && refuelText != null)
            refuelText.gameObject.SetActive(false);
    }
}
