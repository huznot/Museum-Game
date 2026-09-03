using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CompanionMover : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float stopDistance = 0.2f;
    [SerializeField] private float verticalStopDistance = 0.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float maxVelocityChange = 6f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string reachedBool = "hasReached";
    [SerializeField] private bool onlyMoveWhenInWalkState = false;
    [SerializeField] private string walkStateName = "Walk";
    [SerializeField] private bool requireIdleStateForLook = true;
    [SerializeField] private string idleStateName = "Idle";

    [Header("Act 3 Subtitle Look")]
    [SerializeField] private float lookRightYawDegrees = 15f;
    [SerializeField] private float lookRightRotateSpeed = 120f;
    [SerializeField] private bool syncIdleRotationToAct3Dialogue = true;
    [SerializeField] private string firstRotateSubtitleFragment = "spent years telling myself hope was dangerous";
    [SerializeField] private string resetRotateSubtitleFragment = "Because hope makes you believe things can change";
    [SerializeField] private string finalRotateSubtitleFragment = "shown me that hope is a powerful thing";
    [SerializeField] private float idleRotateDelaySeconds = 1.0f;
    [SerializeField] private Vector3 idleTargetEuler = new Vector3(356.569916f, 142.436691f, 358.751892f);
    [SerializeField] private float idleRotateSpeed = 120f;

    private bool hasReached;
    private Rigidbody body;
    private Quaternion lookRightBaseRotation;
    private bool lookRightBaseCached;
    private float idleRotateTimer;
    private Quaternion idleBaseRotation;
    private bool idleBaseRotationCached;
    private int act3IdleRotateState;
    private string lastNonEmptySubtitle;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }
        if (body != null && body.interpolation == RigidbodyInterpolation.None)
        {
            // Smooth visual motion between physics steps.
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        SetReached(false);
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (!hasReached)
        {
            idleRotateTimer = 0f;
            idleBaseRotationCached = false;
            act3IdleRotateState = 0;
            lastNonEmptySubtitle = string.Empty;
            if (onlyMoveWhenInWalkState && animator != null)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(walkStateName))
                {
                    return;
                }
            }

            MoveTowardsTarget();
        }
        else
        {
            UpdateIdleRotation();
        }

        if (!syncIdleRotationToAct3Dialogue)
        {
            UpdateAct3Look();
        }
    }

    private void MoveTowardsTarget()
    {
        if (body == null)
        {
            return;
        }

        Vector3 fullToTarget = target.position - body.position;
        Vector3 toTarget = new Vector3(fullToTarget.x, 0f, fullToTarget.z);

        float distance = toTarget.magnitude;
        if (distance <= stopDistance && Mathf.Abs(fullToTarget.y) <= verticalStopDistance)
        {
            SetReached(true);
            return;
        }

        Vector3 direction = toTarget.normalized;
        Vector3 desiredVelocity = direction * moveSpeed;
        Vector3 currentVelocity = body.linearVelocity;
        Vector3 velocityChange = new Vector3(
            desiredVelocity.x - currentVelocity.x,
            0f,
            desiredVelocity.z - currentVelocity.z
        );

        float maxChange = Mathf.Max(0.01f, maxVelocityChange);
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxChange, maxChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxChange, maxChange);

        body.AddForce(velocityChange, ForceMode.VelocityChange);

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion desired = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion nextRotation = Quaternion.RotateTowards(body.rotation, desired, rotateSpeed * Time.fixedDeltaTime);
            body.MoveRotation(nextRotation);
        }
    }

    private void UpdateAct3Look()
    {
        if (body == null || !ShouldLookRight())
        {
            if (lookRightBaseCached && body != null)
            {
                Quaternion nextRotation = Quaternion.RotateTowards(
                    body.rotation,
                    lookRightBaseRotation,
                    lookRightRotateSpeed * Time.fixedDeltaTime
                );
                body.MoveRotation(nextRotation);
                if (Quaternion.Angle(body.rotation, lookRightBaseRotation) < 0.5f)
                {
                    lookRightBaseCached = false;
                }
            }
            return;
        }

        if (!lookRightBaseCached)
        {
            lookRightBaseRotation = body.rotation;
            lookRightBaseCached = true;
        }

        Quaternion desired = lookRightBaseRotation * Quaternion.Euler(0f, lookRightYawDegrees, 0f);
        Quaternion next = Quaternion.RotateTowards(
            body.rotation,
            desired,
            lookRightRotateSpeed * Time.fixedDeltaTime
        );
        body.MoveRotation(next);
    }

    private void UpdateIdleRotation()
    {
        if (body == null)
        {
            return;
        }

        if (requireIdleStateForLook && animator != null)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(idleStateName))
            {
                idleRotateTimer = 0f;
                return;
            }
        }

        if (!idleBaseRotationCached)
        {
            idleBaseRotation = body.rotation;
            idleBaseRotationCached = true;
        }

        if (syncIdleRotationToAct3Dialogue && TryUpdateAct3IdleRotation())
        {
            return;
        }

        idleRotateTimer += Time.fixedDeltaTime;
        if (idleRotateTimer < Mathf.Max(0f, idleRotateDelaySeconds))
        {
            return;
        }

        Quaternion targetRotation = Quaternion.Euler(idleTargetEuler);
        Quaternion next = Quaternion.RotateTowards(
            body.rotation,
            targetRotation,
            idleRotateSpeed * Time.fixedDeltaTime
        );
        body.MoveRotation(next);
    }

    private bool TryUpdateAct3IdleRotation()
    {
        var mic = MicrophoneInput.MicrophoneManager.Instance;
        if (mic == null || mic.currentAct != MicrophoneInput.MicrophoneManager.Act.Act3)
        {
            return false;
        }

        string currentSubtitle = GetCurrentSubtitle(mic);
        if (!string.IsNullOrEmpty(currentSubtitle))
        {
            lastNonEmptySubtitle = currentSubtitle;
        }

        string subtitleToCheck = !string.IsNullOrEmpty(currentSubtitle) ? currentSubtitle : lastNonEmptySubtitle;

        if (act3IdleRotateState < 1 && SubtitleContains(subtitleToCheck, firstRotateSubtitleFragment))
        {
            act3IdleRotateState = 1;
        }
        else if (act3IdleRotateState < 2 && SubtitleContains(subtitleToCheck, resetRotateSubtitleFragment))
        {
            act3IdleRotateState = 2;
        }
        else if (act3IdleRotateState < 3 && SubtitleContains(subtitleToCheck, finalRotateSubtitleFragment))
        {
            act3IdleRotateState = 3;
        }

        Quaternion targetRotation = (act3IdleRotateState == 1 || act3IdleRotateState == 3)
            ? Quaternion.Euler(idleTargetEuler)
            : idleBaseRotation;

        Quaternion next = Quaternion.RotateTowards(
            body.rotation,
            targetRotation,
            idleRotateSpeed * Time.fixedDeltaTime
        );
        body.MoveRotation(next);
        return true;
    }

    private bool ShouldLookRight()
    {
        if (!hasReached)
        {
            return false;
        }

        if (requireIdleStateForLook && animator != null)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(idleStateName))
            {
                return false;
            }
        }

        var mic = MicrophoneInput.MicrophoneManager.Instance;
        if (mic == null || mic.currentAct != MicrophoneInput.MicrophoneManager.Act.Act3)
        {
            return false;
        }

        if (mic.subtitles == null || mic.subtitles.Length < 2)
        {
            return false;
        }

        string targetSubtitle = mic.subtitles[mic.subtitles.Length - 2];
        if (string.IsNullOrEmpty(targetSubtitle))
        {
            return false;
        }

        string currentSubtitle = GetCurrentSubtitle(mic);

        if (string.IsNullOrEmpty(currentSubtitle))
        {
            return false;
        }

        return currentSubtitle == targetSubtitle;
    }

    private void SetReached(bool reached)
    {
        hasReached = reached;
        if (animator != null && !string.IsNullOrWhiteSpace(reachedBool))
        {
            animator.SetBool(reachedBool, reached);
        }
    }

    private static bool SubtitleContains(string subtitle, string fragment)
    {
        if (string.IsNullOrEmpty(subtitle) || string.IsNullOrEmpty(fragment))
        {
            return false;
        }

        return subtitle.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetCurrentSubtitle(MicrophoneInput.MicrophoneManager mic)
    {
        if (mic == null)
        {
            return string.Empty;
        }

        if (mic.subtitleTextTarget != null)
        {
            return mic.subtitleTextTarget.text;
        }

        if (mic.micInputSystemA != null && mic.micInputSystemA.subtitleTextTarget != null)
        {
            return mic.micInputSystemA.subtitleTextTarget.text;
        }

        if (mic.micInputSystemB != null && mic.micInputSystemB.subtitleTextTarget != null)
        {
            return mic.micInputSystemB.subtitleTextTarget.text;
        }

        return string.Empty;
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawWireSphere(target.position, stopDistance);
    }
}
