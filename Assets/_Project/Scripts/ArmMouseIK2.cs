using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ArmMouseIKRig2 : MonoBehaviour
{
    public Camera cam;

    [Header("Animation Rigging")]
    public TwoBoneIKConstraint rightIK;
    public TwoBoneIKConstraint leftIK;
    public Transform rightTarget;   // same object assigned as Target in rightIK
    public Transform leftTarget;    // same object assigned as Target in leftIK

    [Header("Anchors (children of camera)")]
    public Transform rightAnchor;   // neutral pose for right hand
    public Transform leftAnchor;    // neutral pose for left hand

    [Header("Reach")]
    public float depth = 1.4f;      // plane distance in front of camera
    public float radius = 0.7f;     // max distance from anchor

    [Header("Smoothing")]
    public float posDamping = 12f;  // higher = snappier
    public float weightDamping = 12f;
    public SitInCarController sitController; // assign your player’s SitInCarController

    [Header("Debug")]
    public bool debugLogs = false;
    [Range(0.05f, 1f)] public float debugLogInterval = 0.25f;

    bool rightGrabbed, leftGrabbed;
    float debugTimer = 0f;

    void Update()
    {
        if (sitController != null && sitController.isSitting)
            return; // skip IK updates while sitting in car

        UpdateHand(true,  rightIK, rightAnchor, rightTarget, Input.GetMouseButton(1) || rightGrabbed);
        UpdateHand(false, leftIK,  leftAnchor,  leftTarget,  Input.GetMouseButton(0) || leftGrabbed);
    }

    void UpdateHand(bool isRight, TwoBoneIKConstraint ik, Transform anchor, Transform target, bool active)
    {
        // NEW: use camera forward to define control plane
        Vector3 planeOrigin = cam.transform.position + cam.transform.forward * depth;
        Plane plane = new Plane(cam.transform.forward, planeOrigin);

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Vector3 desired = target.position;
        if (plane.Raycast(ray, out float t))
        {
            Vector3 hit = ray.GetPoint(t);
            Vector3 offset = hit - anchor.position;
            if (offset.magnitude > radius) offset = offset.normalized * radius;
            desired = anchor.position + offset;
        }

        // Smooth target position (no teleport)
        float posLerp = 1f - Mathf.Exp(-posDamping * Time.deltaTime);
        target.position = Vector3.Lerp(target.position, desired, posLerp);

        // Smooth IK weight
        float wTarget = active ? 1f : 0f;
        float wLerp = 1f - Mathf.Exp(-weightDamping * Time.deltaTime);
        ik.weight = Mathf.Lerp(ik.weight, wTarget, wLerp);

        // Optional: keep target oriented with camera forward
        target.rotation = Quaternion.Slerp(
            target.rotation,
            Quaternion.LookRotation(anchor.forward, anchor.up),
            posLerp
        );

        if (debugLogs)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= debugLogInterval)
            {
                debugTimer = 0f;
                Debug.Log($"[ArmMouseIKRig2] {(isRight ? "Right" : "Left")} active={active} targetPos={target.position} desired={desired} anchor={anchor.position} weight={ik.weight}");
            }
        }
    }

    public void SetGrabbed(bool rightHand, bool isGrabbed)
    {
        if (rightHand) rightGrabbed = isGrabbed; else leftGrabbed = isGrabbed;
    }
}
