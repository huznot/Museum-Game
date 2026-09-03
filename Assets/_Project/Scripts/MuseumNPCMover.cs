using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MuseumNPCMover : MonoBehaviour
{
    private enum GuideState { Idle, Walking, Talking, TourComplete }

    // Rotation is a separate state machine so it never fights FixedUpdate
    private enum RotState
    {
        None,            // walking — UpdateMovement handles facing
        LockedToPlayer,  // intro: always face player
        SlerpToDisplay,  // arriving at display: slerp toward defaultLookAt
        HoldDisplay,     // holding display direction, counting down to glance
        SlerpToPlayer,   // glance: slerp toward player
        TrackPlayer,     // glance: hold and follow player movement
        SlerpBack        // glance: slerp back to display direction
    }

    [Header("Guide Flow")]
    [SerializeField] private bool startSequenceOnStart = true;
    [SerializeField] private float introStartDelay = 1.25f;
    [SerializeField] private float dialogueStartDelay = 0.35f;

    [Header("Movement")]
    [SerializeField] private float stopDistance = 0.2f;
    [SerializeField] private float verticalStopDistance = 0.75f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float rotateSpeed = 180f;

    [Header("Idle Look Behaviour")]
    public Transform greetPlayer;
    [SerializeField] private Vector2 lookAtPlayerIntervalRange = new Vector2(6f, 12f);
    [SerializeField] private Vector2 lookAtPlayerHoldRange = new Vector2(3f, 5f);
    [SerializeField] private float lookSlerpDuration = 1.5f;
    [SerializeField] private float playerTrackingSpeed = 120f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingBoolName = "isWalking";
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Dialogue")]
    [SerializeField] private MicrophoneInput.MuseumMicrophone museumMicrophone;

    // ── Runtime ───────────────────────────────────────────────────────────────────

    private Rigidbody body;
    private GuideState state;
    private Transform currentTarget;
    private Coroutine masterRoutine;

    // Rotation state machine
    private RotState rotState = RotState.None;
    private Quaternion _displayRot;   // the direction to face at this display
    private Quaternion _rotFrom, _rotTo;
    private float _rotElapsed, _rotDuration;
    private float _glanceTimer;
    private float _holdTimer;

    public bool IsWalking => state == GuideState.Walking;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────

    private void Reset() { animator = GetComponent<Animator>(); }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.angularDamping = Mathf.Infinity; // prevent physics from ever spinning the NPC
        }
    }

    private void Start()
    {
        if (museumMicrophone == null)
            museumMicrophone = MicrophoneInput.MuseumMicrophone.Instance;

        // Prevent player from physically pushing the NPC
        if (greetPlayer != null)
        {
            var npcCols    = GetComponentsInChildren<Collider>();
            var playerCols = greetPlayer.root.GetComponentsInChildren<Collider>();
            foreach (var nc in npcCols)
                foreach (var pc in playerCols)
                    Physics.IgnoreCollision(nc, pc, true);
        }

        DeactivateAllForcefields();
        state = GuideState.Idle;
        SetWalking(false);

        if (startSequenceOnStart)
            masterRoutine = StartCoroutine(MasterTourRoutine());
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case GuideState.Walking:
                UpdateMovement();
                break;
            case GuideState.Talking:
                UpdateRotationStateMachine();
                break;
        }
    }

    // ── Master tour coroutine ─────────────────────────────────────────────────────

    private IEnumerator MasterTourRoutine()
    {
        if (introStartDelay > 0f)
            yield return new WaitForSeconds(introStartDelay);

        // 1. Walk to intro greeting position
        if (museumMicrophone != null)
        {
            GameObject introTarget = museumMicrophone.GetIntroWalkTarget();
            if (introTarget != null)
            {
                Log($"Walking to intro position: {introTarget.name}");
                yield return StartCoroutine(WalkTo(introTarget.transform));
            }
        }

        // 2. Intro lines — lock onto player the whole time
        SetRotLocked();
        if (dialogueStartDelay > 0f) yield return new WaitForSeconds(dialogueStartDelay);
        if (museumMicrophone != null && museumMicrophone.PlayIntroLines())
        {
            Log("Playing intro lines");
            while (museumMicrophone.IsDialoguePlaying) yield return null;
        }
        SetRotNone();

        // 3. Run each act
        int actCount = museumMicrophone != null ? museumMicrophone.ActCount : 0;
        for (int a = 0; a < actCount; a++)
        {
            ActivateForcefield(a);

            float actDelay = museumMicrophone != null ? museumMicrophone.GetActStartDelay(a) : 0f;
            if (actDelay > 0f) yield return new WaitForSeconds(actDelay);

            int subCount = museumMicrophone != null ? museumMicrophone.GetSubDisplayCount(a) : 0;
            for (int s = 0; s < subCount; s++)
            {
                Transform spot = museumMicrophone != null ? museumMicrophone.GetSubDisplaySpot(a, s) : null;
                if (spot != null)
                {
                    Log($"Act {a}, SubDisplay {s}: walking to '{spot.name}'");
                    yield return StartCoroutine(WalkTo(spot));
                }

                Transform defaultLook = museumMicrophone != null
                    ? museumMicrophone.GetSubDisplayDefaultLookAt(a, s) : null;
                EnterIdleStance(defaultLook);

                if (dialogueStartDelay > 0f) yield return new WaitForSeconds(dialogueStartDelay);

                if (museumMicrophone != null && museumMicrophone.PlaySubDisplayDialogue(a, s))
                {
                    Log($"Act {a}, SubDisplay {s}: talking");
                    while (museumMicrophone.IsDialoguePlaying) yield return null;
                }

                SetRotNone();
            }

        }

        CompleteTour();
    }

    // ── Movement ──────────────────────────────────────────────────────────────────

    private IEnumerator WalkTo(Transform target)
    {
        SetRotNone();
        currentTarget = target;
        state = GuideState.Walking;
        SetWalking(true);

        while (!IsCloseEnough(target))
            yield return new WaitForFixedUpdate();

        // Kill leftover velocity so the NPC stops cleanly
        if (body != null)
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);

        currentTarget = null;
        state = GuideState.Talking;
        SetWalking(false);
        Log($"Reached '{target.name}'");
    }

    private bool IsCloseEnough(Transform target)
    {
        if (target == null || body == null) return true;
        Vector3 diff = target.position - body.position;
        float flatDist = new Vector2(diff.x, diff.z).magnitude;
        return flatDist <= stopDistance && Mathf.Abs(diff.y) <= verticalStopDistance;
    }

    private void UpdateMovement()
    {
        if (body == null || currentTarget == null) return;

        Vector3 diff = currentTarget.position - body.position;
        Vector3 flat = new Vector3(diff.x, 0f, diff.z);
        if (flat.sqrMagnitude < 0.0001f) return;

        Vector3 dir = flat.normalized;
        Vector3 velChange = new Vector3(
            dir.x * moveSpeed - body.linearVelocity.x,
            0f,
            dir.z * moveSpeed - body.linearVelocity.z);
        body.AddForce(velChange, ForceMode.VelocityChange);

        // Face movement direction while walking only
        Quaternion desired = Quaternion.LookRotation(dir, Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, desired, rotateSpeed * Time.fixedDeltaTime));
    }

    // ── Rotation state machine (runs in FixedUpdate — no coroutines) ──────────────

    private void EnterIdleStance(Transform defaultFacing)
    {
        state = GuideState.Talking;

        if (defaultFacing != null && body != null)
        {
            // Slerp smoothly to face the display
            Vector3 dir = defaultFacing.position - body.position;
            dir.y = 0f;
            _displayRot = dir.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(dir.normalized, Vector3.up)
                : body.rotation;

            _rotFrom    = body.rotation;
            _rotTo      = _displayRot;
            _rotElapsed = 0f;
            _rotDuration = lookSlerpDuration;
            rotState    = RotState.SlerpToDisplay;
        }
        else
        {
            // No defaultLookAt — hold wherever the NPC stopped, then start glance timer
            _displayRot = body != null ? body.rotation : transform.rotation;
            _glanceTimer = Random.Range(lookAtPlayerIntervalRange.x, lookAtPlayerIntervalRange.y);
            rotState = RotState.HoldDisplay;
        }
    }

    private void SetRotLocked()  => rotState = RotState.LockedToPlayer;
    private void SetRotNone()    => rotState = RotState.None;

    private void UpdateRotationStateMachine()
    {
        if (body == null) return;

        switch (rotState)
        {
            // ── Slerp toward the display's defaultLookAt ──────────────────────
            case RotState.SlerpToDisplay:
                _rotElapsed += Time.fixedDeltaTime;
                body.MoveRotation(Quaternion.Slerp(_rotFrom, _rotTo,
                    Mathf.Clamp01(_rotElapsed / _rotDuration)));
                if (_rotElapsed >= _rotDuration)
                {
                    body.MoveRotation(_displayRot);
                    _glanceTimer = Random.Range(lookAtPlayerIntervalRange.x, lookAtPlayerIntervalRange.y);
                    rotState = RotState.HoldDisplay;
                }
                break;

            // ── Facing display, counting down until next glance ───────────────
            case RotState.HoldDisplay:
                body.MoveRotation(_displayRot);
                if (greetPlayer == null) break;
                _glanceTimer -= Time.fixedDeltaTime;
                if (_glanceTimer > 0f) break;

                Vector3 toPlayer = greetPlayer.position - body.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude < 0.0001f)
                {
                    _glanceTimer = Random.Range(lookAtPlayerIntervalRange.x, lookAtPlayerIntervalRange.y);
                    break;
                }
                _rotFrom    = body.rotation;
                _rotTo      = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                _rotElapsed = 0f;
                _rotDuration = lookSlerpDuration;
                rotState    = RotState.SlerpToPlayer;
                break;

            // ── Slerp toward player ───────────────────────────────────────────
            case RotState.SlerpToPlayer:
                _rotElapsed += Time.fixedDeltaTime;
                body.MoveRotation(Quaternion.Slerp(_rotFrom, _rotTo,
                    Mathf.Clamp01(_rotElapsed / _rotDuration)));
                if (_rotElapsed >= _rotDuration)
                {
                    _holdTimer = Random.Range(lookAtPlayerHoldRange.x, lookAtPlayerHoldRange.y);
                    rotState   = RotState.TrackPlayer;
                }
                break;

            // ── Track player movement for holdTimer seconds ───────────────────
            case RotState.TrackPlayer:
                if (greetPlayer != null)
                {
                    Vector3 tp = greetPlayer.position - body.position;
                    tp.y = 0f;
                    if (tp.sqrMagnitude > 0.0001f)
                    {
                        Quaternion tgt = Quaternion.LookRotation(tp.normalized, Vector3.up);
                        body.MoveRotation(Quaternion.RotateTowards(body.rotation, tgt,
                            playerTrackingSpeed * Time.fixedDeltaTime));
                    }
                }
                _holdTimer -= Time.fixedDeltaTime;
                if (_holdTimer <= 0f)
                {
                    _rotFrom    = body.rotation;
                    _rotTo      = _displayRot;
                    _rotElapsed = 0f;
                    _rotDuration = lookSlerpDuration;
                    rotState    = RotState.SlerpBack;
                }
                break;

            // ── Slerp back to display direction ───────────────────────────────
            case RotState.SlerpBack:
                _rotElapsed += Time.fixedDeltaTime;
                body.MoveRotation(Quaternion.Slerp(_rotFrom, _rotTo,
                    Mathf.Clamp01(_rotElapsed / _rotDuration)));
                if (_rotElapsed >= _rotDuration)
                {
                    body.MoveRotation(_displayRot);
                    _glanceTimer = Random.Range(lookAtPlayerIntervalRange.x, lookAtPlayerIntervalRange.y);
                    rotState     = RotState.HoldDisplay;
                }
                break;

            // ── Intro: always face player ─────────────────────────────────────
            case RotState.LockedToPlayer:
                if (greetPlayer == null) break;
                Vector3 toP = greetPlayer.position - body.position;
                toP.y = 0f;
                if (toP.sqrMagnitude > 0.0001f)
                {
                    Quaternion tgt = Quaternion.LookRotation(toP.normalized, Vector3.up);
                    body.MoveRotation(Quaternion.RotateTowards(body.rotation, tgt,
                        playerTrackingSpeed * Time.fixedDeltaTime));
                }
                break;
        }
    }

    // ── Forcefields ───────────────────────────────────────────────────────────────

    private void ActivateForcefield(int actIndex)
    {
        DeactivateAllForcefields();
        GameObject ff = museumMicrophone != null ? museumMicrophone.GetActForcefield(actIndex) : null;
        if (ff != null) ff.SetActive(true);
    }

    private void DeactivateAllForcefields()
    {
        if (museumMicrophone == null) return;
        for (int i = 0; i < museumMicrophone.ActCount; i++)
        {
            GameObject ff = museumMicrophone.GetActForcefield(i);
            if (ff != null) ff.SetActive(false);
        }
    }

    // ── Tour complete ─────────────────────────────────────────────────────────────

    private void CompleteTour()
    {
        SetRotNone();
        DeactivateAllForcefields();
        state = GuideState.TourComplete;
        SetWalking(false);
        Log("Tour complete");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private void SetWalking(bool walking)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(walkingBoolName))
            animator.SetBool(walkingBoolName, walking);
    }

    private void Log(string msg)
    {
        if (enableDebugLogs)
            Debug.Log($"[MuseumNPCMover] {name}: {msg} | state={state} rot={rotState}");
    }

    private void OnDrawGizmosSelected()
    {
        if (currentTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, stopDistance);
        }

        if (museumMicrophone == null) return;
        for (int a = 0; a < museumMicrophone.ActCount; a++)
        {
            Gizmos.color = Color.HSVToRGB((float)a / Mathf.Max(1, museumMicrophone.ActCount), 0.8f, 1f);
            Transform prev = null;
            for (int s = 0; s < museumMicrophone.GetSubDisplayCount(a); s++)
            {
                Transform spot = museumMicrophone.GetSubDisplaySpot(a, s);
                if (spot == null) continue;
                Gizmos.DrawWireSphere(spot.position, 0.25f);
                if (prev != null) Gizmos.DrawLine(prev.position, spot.position);
                prev = spot;
            }
        }
    }
}
