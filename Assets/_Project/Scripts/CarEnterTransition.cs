using UnityEngine;
using UnityTutorial.PlayerControl;
using UnityEngine.Animations.Rigging;
using System.Collections;

public class SitTransition : MonoBehaviour
{
    [Header("Player & Car Setup")]
    public Transform player;                 
    public Transform sitPos;
    public GameObject carGameObject;
    public Animator animator;
    public Transform exitPos;
    public BoxCollider enterAreaCollider;
    public bool debugLogs = true;

    [Header("Enter Prompt")]
    public GameObject aimPrompt;

    [Header("Original IK (unchanged)")]
    public TwoBoneIKConstraint LeftArmIk;
    public TwoBoneIKConstraint RightArmIk;

    [Header("Pre-animation IK (for door animation)")]
    public TwoBoneIKConstraint PreAnimLeftIK;    // separate IK for hand animation
    public TwoBoneIKConstraint PreAnimRightIK;   // separate IK for hand animation
    public Transform PreAnimLeftTarget;          // target for left hand
    public Transform PreAnimRightTarget;         // target for right hand
    public Transform DoorHandle;                 // door handle transform
    public Transform Door;                       // door transform to rotate
    public float DoorOpenAngle = 70f;
    public float PreAnimDuration = 0.6f;
    public float DoorOpenSpeed = 2f;

    [Header("Pre-Exit Animation")]
    public Transform DoorHandleInside;           // inside door handle (can be same as DoorHandle if needed)
    public float PreExitDuration = 0.8f;

    [Header("Post-Exit Animation")]
    public Transform PostExitHandTarget;         // specific target for post-exit door closing hand position

    [Header("Smooth Exit Settings")]
    public float exitMoveSpeed = 3f;
    public float exitRotateSpeed = 5f;

    [Header("Optional Components")]
    public CarController carController;

    [Header("Audio")]
    public AudioClip carDoorOpenClip;
    public AudioClip carDoorCloseClip;
    public AudioSource carDoorAudioSource;
    [Range(0f, 1f)] public float carDoorVolume = 0.9f;
    public float carDoorPitch = 1f;
    [Tooltip("Degrees from the closed rotation before the close sound plays.")]
    public float doorCloseSoundAngleThreshold = 3f;

    private bool _sitting = true;
    private bool _exitingCar = false;
    private bool _isPreAnimating = false;
    private bool _isPreExiting = false;
    public float leanAmount = 0.2f; // how much to lean toward door when reaching
    private Quaternion _doorClosedLocalRotation;

    void Awake()
    {
        if (aimPrompt != null)
            aimPrompt.SetActive(false);

        EnsureAudioSource();
        if (Door != null)
            _doorClosedLocalRotation = Door.localRotation;
    }

    void Update()
    {
        UpdatePrompt();

        var playerController = player != null ? player.GetComponent<PlayerController>() : null;

        // ENTER CAR
        if (Input.GetKeyDown(KeyCode.E) && !_sitting && !_exitingCar)
        {
            if (!IsPlayerInArea())
            {
                if (debugLogs) Debug.Log("❌ Not in enter area — move into the box to enter the car.");
                return;
            }

            if (playerController != null && playerController.IsMoving())
            {
                if (debugLogs) Debug.Log("❌ Stop moving before entering the car!");
                return;
            }

            if (!_isPreAnimating)
            {
                StartCoroutine(PreEnterAnimation());
            }
        }
        // EXIT CAR
        else if (Input.GetKeyDown(KeyCode.E) && _sitting && !_exitingCar && !_isPreExiting)
        {
            if (carController != null && carController.GetSpeed() > 0.1f)
            {
                Debug.Log("❌ Stop the car before exiting!");
                return;
            }

            StartCoroutine(PreExitAnimation());
        }

        if (_exitingCar)
        {
            UpdateExitMovement();
        }
    }

    private bool IsPlayerInArea(bool log = true)
    {
        if (enterAreaCollider == null || player == null) return false;

        Vector3 worldCenter = enterAreaCollider.transform.TransformPoint(enterAreaCollider.center);
        Vector3 halfExtents = Vector3.Scale(enterAreaCollider.size * 0.5f, enterAreaCollider.transform.lossyScale);
        Quaternion orientation = enterAreaCollider.transform.rotation;

        Collider[] hits = Physics.OverlapBox(worldCenter, halfExtents, orientation);
        if (log && debugLogs) Debug.Log($"OverlapBox found {hits.Length} collider(s).");

        foreach (var c in hits)
        {
            if (c == null) continue;
            if (c.transform == player || c.transform.root == player
                || c.CompareTag("Player") || c.GetComponentInParent<PlayerController>() != null)
            {
                if (log && debugLogs) Debug.Log("Overlap hit: " + c.name);
                return true;
            }
        }

        return false;
    }

    private void UpdatePrompt()
    {
        if (aimPrompt == null) return;

        bool show = !_sitting
            && !_exitingCar
            && !_isPreAnimating
            && !_isPreExiting
            && IsPlayerInArea(false);

        if (aimPrompt.activeSelf != show)
            aimPrompt.SetActive(show);
    }

    private IEnumerator PreExitAnimation()
    {
        _isPreExiting = true;
        PlayDoorSound(carDoorOpenClip);
        
        if (debugLogs) Debug.Log("Starting pre-exit door opening animation...");

        // Use inside door handle if available, otherwise use the main door handle
        Transform targetHandle = DoorHandleInside != null ? DoorHandleInside : DoorHandle;

        if (PreAnimLeftIK == null || PreAnimLeftTarget == null || targetHandle == null || Door == null)
        {
            Debug.LogError("Pre-exit animation components not assigned! Skipping to direct exit.");
            StartExitCar();
            yield break;
        }

        // Disable main IK temporarily so pre-anim IK can work
        float originalLeftIKWeight = 0f;
        if (LeftArmIk != null)
        {
            originalLeftIKWeight = LeftArmIk.weight;
            LeftArmIk.weight = 0f;
        }

        // Store starting values
        Vector3 startPos = PreAnimLeftTarget.position;
        Vector3 handlePos = targetHandle.position;
        float startWeight = PreAnimLeftIK.weight;

        // Door rotation values
        Quaternion doorStartRot = Door.localRotation; // Currently closed
        Quaternion doorTargetRot = Quaternion.Euler(Door.localEulerAngles.x, Door.localEulerAngles.y, DoorOpenAngle);

        float elapsed = 0f;

        // Phase 1: Reach for inside door handle
        float reachDuration = PreExitDuration * 0.4f; // 40% of total time
        
        // Calculate natural reach from inside car to door handle
        Vector3 playerPos = player.position;
        Vector3 reachDirection = (handlePos - playerPos).normalized;
        
        // Create smooth arc motion to handle
        Vector3 midPoint = Vector3.Lerp(startPos, handlePos, 0.5f);
        midPoint += player.up * 0.08f; // Slight upward arc for natural motion
        midPoint += player.right * 0.1f; // Slight outward motion toward door

        while (elapsed < reachDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / reachDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentPos;
            if (t < 0.5f)
            {
                // First half: start to midpoint
                currentPos = Vector3.Lerp(startPos, midPoint, t * 2f);
            }
            else
            {
                // Second half: midpoint to handle
                currentPos = Vector3.Lerp(midPoint, handlePos, (t - 0.5f) * 2f);
            }

            PreAnimLeftTarget.position = currentPos;
            PreAnimLeftIK.weight = Mathf.Lerp(startWeight, 1f, t);

            yield return null;
        }

        // Ensure hand is exactly at handle
        PreAnimLeftTarget.position = handlePos;
        PreAnimLeftIK.weight = 1f;

        // Phase 2: Push/pull door open
        float openDuration = PreExitDuration * 0.6f; // 60% of total time
        elapsed = 0f;

        // Calculate push motion (pushing door outward from inside)
        Vector3 doorToOutside = Door.right; // Assuming door opens outward along its right axis
        Vector3 pushDirection = doorToOutside + player.forward * 0.2f; // Push outward and slightly forward
        Vector3 pushEndPos = handlePos + pushDirection * 0.35f;
        pushEndPos.y += 0.02f; // Slight upward motion while pushing

        // Mid-point for natural pushing arc
        Vector3 midPushPos = Vector3.Lerp(handlePos, pushEndPos, 0.5f);
        midPushPos += doorToOutside * 0.15f; // Extra outward motion at mid-point

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            // Smooth arc motion for pushing door open
            Vector3 currentPos;
            if (t < 0.5f)
            {
                currentPos = Vector3.Lerp(handlePos, midPushPos, t * 2f);
            }
            else
            {
                currentPos = Vector3.Lerp(midPushPos, pushEndPos, (t - 0.5f) * 2f);
            }

            PreAnimLeftTarget.position = currentPos;
            
            // Open door with smooth motion
            Door.localRotation = Quaternion.Slerp(doorStartRot, doorTargetRot, t);

            yield return null;
        }

        // Phase 3: Brief settle/release (makes it feel more natural)
        yield return new WaitForSeconds(0.1f);

        // Phase 4: Return hand to natural position
        float releaseDuration = 0.3f;
        elapsed = 0f;
        Vector3 releaseStartPos = PreAnimLeftTarget.position;
        Vector3 naturalRestPos = startPos;

        while (elapsed < releaseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / releaseDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            PreAnimLeftTarget.position = Vector3.Lerp(releaseStartPos, naturalRestPos, t);
            // Gradually reduce IK weight
            PreAnimLeftIK.weight = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        // Reset pre-animation IK
        PreAnimLeftIK.weight = 0f;

        if (debugLogs) Debug.Log("✅ Pre-exit door animation complete, now exiting car...");

        _isPreExiting = false;

        // Now proceed with the original exit logic
        StartExitCar();
    }

private IEnumerator CloseDoorAnimation()
{
    if (debugLogs) Debug.Log("Starting door closing animation...");

    // Override main IK temporarily - disable it so pre-anim IK can work
    float originalLeftIKWeight = 0f;
    if (LeftArmIk != null)
    {
        originalLeftIKWeight = LeftArmIk.weight;
        LeftArmIk.weight = 0f; // Disable main IK
    }

    // Use the outside door handle (since DoorHandleInside might not be defined yet)
    Transform targetHandle = DoorHandle;

    // Door is currently open, we need to close it
    Quaternion doorOpenRot = Door.localRotation; // Current open position
    Quaternion doorClosedRot = _doorClosedLocalRotation; // Closed position (cached)
    bool closeSoundPlayed = false;

    // Starting position for hand (from inside the car)
    Vector3 startPos = PreAnimLeftTarget.position;
    Vector3 handlePos = targetHandle.position;

    float elapsed = 0f;

    // Phase 1: Reach for door handle from inside (reverse arc motion)
    float reachDuration = 0.5f;
    
    // Calculate natural reach from inside car
    Vector3 playerPos = player.position;
    Vector3 reachDirection = (handlePos - playerPos).normalized;
    Vector3 midReachPos = Vector3.Lerp(startPos, handlePos, 0.5f);
    midReachPos += player.up * 0.08f; // Slight upward arc

    while (elapsed < reachDuration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / reachDuration);
        t = Mathf.SmoothStep(0f, 1f, t);

        Vector3 currentPos;
        if (t < 0.5f)
        {
            currentPos = Vector3.Lerp(startPos, midReachPos, t * 2f);
        }
        else
        {
            currentPos = Vector3.Lerp(midReachPos, handlePos, (t - 0.5f) * 2f);
        }

        PreAnimLeftTarget.position = currentPos;
        PreAnimLeftIK.weight = Mathf.Lerp(0.2f, 1f, t); // Start from low weight

        yield return null;
    }

    // Ensure at handle
    PreAnimLeftTarget.position = handlePos;
    PreAnimLeftIK.weight = 1f;

    // Phase 2: Brief pause at handle (door stays open)
    yield return new WaitForSeconds(0.2f);

    // Phase 3: Pull door closed while pulling hand back (door closes as hand moves)
    float closeDuration = 0.8f;
    elapsed = 0f;

    Vector3 doorToPlayer = (playerPos - Door.position).normalized;
    Vector3 pullDirection = doorToPlayer; // Pull inward and slightly across body
    Vector3 pullEndPos = handlePos + pullDirection * 0.3f;
    pullEndPos.y -= 0.03f; // Slight downward motion

    // Mid-point for natural arc
    Vector3 midPullPos = Vector3.Lerp(handlePos, pullEndPos, 0.5f);
    midPullPos -= player.right * 0.15f; // Extra inward motion at mid-point

    while (elapsed < closeDuration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / closeDuration);
        t = Mathf.SmoothStep(0f, 1f, t);

        // Smooth arc for pulling door closed
        Vector3 currentPos;
        if (t < 0.5f)
        {
            currentPos = Vector3.Lerp(handlePos, midPullPos, t * 2f);
        }
        else
        {
            currentPos = Vector3.Lerp(midPullPos, pullEndPos, (t - 0.5f) * 2f);
        }

        PreAnimLeftTarget.position = currentPos;
        
        // Close door simultaneously as hand pulls back (synchronized motion)
        Door.localRotation = Quaternion.Slerp(doorOpenRot, doorClosedRot, t);
        if (!closeSoundPlayed && Quaternion.Angle(Door.localRotation, doorClosedRot) <= doorCloseSoundAngleThreshold)
        {
            PlayDoorSound(carDoorCloseClip);
            closeSoundPlayed = true;
        }

        yield return null;
    }

    // Phase 3: Smooth transition to steering wheel position
    yield return StartCoroutine(TransitionToSteeringWheel());

    if (debugLogs) Debug.Log("Door closing animation complete!");
}

private IEnumerator TransitionToSteeringWheel()
{
    if (debugLogs) Debug.Log("Transitioning hand to steering wheel...");

    // Get the target position from the main IK system (steering wheel)
    SitInCarController sitController = player.GetComponent<SitInCarController>();
    Transform steeringWheelTarget = null;
    
    if (sitController != null && sitController.leftHandTarget != null)
    {
        steeringWheelTarget = sitController.leftHandTarget;
    }

    if (steeringWheelTarget == null)
    {
        if (debugLogs) Debug.LogWarning("No steering wheel target found for smooth transition!");
        yield break; // Use yield break instead of return
    }

    Vector3 startPos = PreAnimLeftTarget.position;
    Vector3 targetPos = steeringWheelTarget.position;
    Quaternion startRot = PreAnimLeftTarget.rotation;
    Quaternion targetRot = steeringWheelTarget.rotation;

    float transitionDuration = 0.6f;
    float elapsed = 0f;

    // Create a smooth arc to steering wheel
    Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
    midPoint += player.up * 0.1f; // Slight upward arc for natural motion

    while (elapsed < transitionDuration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / transitionDuration);
        t = Mathf.SmoothStep(0f, 1f, t);

        // Smooth arc motion to steering wheel
        Vector3 currentPos;
        if (t < 0.5f)
        {
            currentPos = Vector3.Lerp(startPos, midPoint, t * 2f);
        }
        else
        {
            currentPos = Vector3.Lerp(midPoint, targetPos, (t - 0.5f) * 2f);
        }

        PreAnimLeftTarget.position = currentPos;
        PreAnimLeftTarget.rotation = Quaternion.Slerp(startRot, targetRot, t);

        // Gradually transition IK weights
        float inverseT = 1f - t;
        PreAnimLeftIK.weight = inverseT; // Fade out pre-anim IK
        
        if (LeftArmIk != null)
        {
            LeftArmIk.weight = t; // Fade in main IK
        }

        yield return null;
    }

    // Ensure final transition
    PreAnimLeftIK.weight = 0f;
    if (LeftArmIk != null) LeftArmIk.weight = 1f;

    if (debugLogs) Debug.Log("Hand smoothly transitioned to steering wheel!");
}

private IEnumerator PreEnterAnimation()
{
    _isPreAnimating = true;
    PlayDoorSound(carDoorOpenClip);

    if (PreAnimLeftIK == null || PreAnimLeftTarget == null || DoorHandle == null || Door == null)
    {
        Debug.LogError("Pre-animation IK or DoorHandle/Door not assigned!");
        yield break;
    }

    // Store starting values
    Vector3 startPos = PreAnimLeftTarget.position;
    Vector3 handlePos = DoorHandle.position;
    float startWeight = PreAnimLeftIK.weight;

    // Store original player position for leaning
    Vector3 originalPlayerPos = player.position;
    Vector3 leanDirection = (handlePos - player.position).normalized;
    leanDirection.y = 0; // Keep lean horizontal only
    Vector3 leanTargetPos = originalPlayerPos + leanDirection * leanAmount;

    Quaternion doorStartRot = Door.localRotation;
    Quaternion doorTargetRot = Quaternion.Euler(Door.localEulerAngles.x, Door.localEulerAngles.y, DoorOpenAngle);

    float elapsed = 0f;

    // Phase 1: Lean toward door while reaching for handle
    float reachDuration = PreAnimDuration * 0.4f; // 40% of total time
    while (elapsed < reachDuration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / reachDuration);
        t = Mathf.SmoothStep(0f, 1f, t);

        // Lean player toward door
        player.position = Vector3.Lerp(originalPlayerPos, leanTargetPos, t);

        // Add slight upward arc to make reaching motion more natural
        Vector3 midPoint = Vector3.Lerp(startPos, handlePos, 0.5f);
        midPoint.y += 0.1f; // Lift hand slightly during reach

        Vector3 currentPos;
        if (t < 0.5f)
        {
            // First half: start to midpoint
            currentPos = Vector3.Lerp(startPos, midPoint, t * 2f);
        }
        else
        {
            // Second half: midpoint to handle
            currentPos = Vector3.Lerp(midPoint, handlePos, (t - 0.5f) * 2f);
        }

        PreAnimLeftTarget.position = currentPos;
        PreAnimLeftIK.weight = Mathf.Lerp(startWeight, 1f, t);

        yield return null;
    }

    // Ensure hand is exactly at handle and player is leaned
    PreAnimLeftTarget.position = handlePos;
    PreAnimLeftIK.weight = 1f;
    player.position = leanTargetPos;

    // Phase 2: Pull handle and swing arm back while maintaining lean
    float pullDuration = PreAnimDuration * 0.6f; // 60% of total time
    elapsed = 0f;

    // Calculate realistic pull-back positions (adjusted for leaned position)
    Vector3 playerLeanedPos = player.position;
    Vector3 doorToPlayer = (playerLeanedPos - Door.position).normalized;
    
    // Create an arc motion - hand moves back and slightly outward
    Vector3 pullDirection = doorToPlayer + player.right * 0.3f; // Add some sideways motion
    Vector3 pullBackPos = handlePos + pullDirection * 0.4f; // Pull back further
    pullBackPos.y -= 0.05f; // Slight downward motion as you pull

    // Optional: Calculate intermediate positions for more natural arc
    Vector3 midPullPos = Vector3.Lerp(handlePos, pullBackPos, 0.5f);
    midPullPos += player.right * 0.1f; // Extra outward motion at mid-point

    while (elapsed < pullDuration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / pullDuration);
        t = Mathf.SmoothStep(0f, 1f, t);

        // Create smooth arc motion for pulling
        Vector3 currentPos;
        if (t < 0.5f)
        {
            // First half of pull: handle to mid-pull position
            currentPos = Vector3.Lerp(handlePos, midPullPos, t * 2f);
        }
        else
        {
            // Second half: mid-pull to final pull position
            currentPos = Vector3.Lerp(midPullPos, pullBackPos, (t - 0.5f) * 2f);
        }

        PreAnimLeftTarget.position = currentPos;
        
        // Open door with eased motion
        Door.localRotation = Quaternion.Slerp(doorStartRot, doorTargetRot, t);

        yield return null;
    }

    // Phase 3: Brief pause/settle (optional - makes it feel more natural)
    yield return new WaitForSeconds(0.1f);

    // Phase 4: Return to original position while moving hand to natural position
    float releaseDuration = 0.3f;
    elapsed = 0f;
    Vector3 releaseStartPos = PreAnimLeftTarget.position;
    Vector3 naturalRestPos = startPos + player.forward * 0.2f; // Slightly forward from start

    while (elapsed < releaseDuration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / releaseDuration);
        t = Mathf.SmoothStep(0f, 1f, t);

        // Return player to original position
        player.position = Vector3.Lerp(leanTargetPos, originalPlayerPos, t);

        PreAnimLeftTarget.position = Vector3.Lerp(releaseStartPos, naturalRestPos, t);
        // Gradually reduce IK weight to let arm return to natural pose
        PreAnimLeftIK.weight = Mathf.Lerp(1f, 0.2f, t);

        yield return null;
    }

    // Ensure player is back at original position
    player.position = originalPlayerPos;

    // Don't reset IK yet - we need it for door closing
    
    // Trigger original sitting logic FIRST
    SitInCarController sitController = player.GetComponent<SitInCarController>();
    if (sitController != null)
    {
        sitController.isSitting = true;
        sitController.enabled = true;
    }

    _sitting = true;

    if (debugLogs) Debug.Log("✅ Entered car, now closing door...");

    // Start door closing animation
    yield return StartCoroutine(CloseDoorAnimation());

    _isPreAnimating = false;

    if (debugLogs) Debug.Log("✅ Door closed, ready to drive!");
}


    // ------------------- Original Exit Logic -------------------
    private void StartExitCar()
    {
        var sitController = player.GetComponent<SitInCarController>();
        var playerController = player.GetComponent<PlayerController>();
        var playerRigidbody = player.GetComponent<Rigidbody>();

        if (sitController != null) sitController.isSitting = false;
        if (sitController != null) sitController.enabled = false;
        _sitting = false;
        _exitingCar = true;

        if (carController != null)
        {
            carController.SetSitInCarController(null);
            carController.enabled = false;
        }

        if (player != null)
        {
            player.SetParent(null);
            if (playerRigidbody != null)
            {
                playerRigidbody.isKinematic = true;
                playerRigidbody.useGravity = false;
                playerRigidbody.detectCollisions = false;
            }
        }

        if (LeftArmIk != null) LeftArmIk.weight = 0f;
        if (RightArmIk != null) RightArmIk.weight = 0f;

        if (playerController != null) playerController.movementDisabled = true;
    }

    private void UpdateExitMovement()
    {
        player.position = Vector3.Lerp(player.position, exitPos.position, Time.deltaTime * exitMoveSpeed);
        Quaternion exitYawRotation = GetYawOnlyRotation(exitPos.rotation, player.rotation);
        player.rotation = Quaternion.Slerp(player.rotation, exitYawRotation, Time.deltaTime * exitRotateSpeed);

        if (Vector3.Distance(player.position, exitPos.position) < 0.05f &&
            Quaternion.Angle(player.rotation, exitYawRotation) < 1f)
        {
            CompleteExit();
        }
    }

    private void CompleteExit()
    {
        var playerController = player.GetComponent<PlayerController>();
        var playerRigidbody = player.GetComponent<Rigidbody>();

        player.position = exitPos.position;
        player.rotation = GetYawOnlyRotation(exitPos.rotation, player.rotation);

        animator.applyRootMotion = true;
        animator.CrossFade("Idle", 0.05f);

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.useGravity = true;
            playerRigidbody.detectCollisions = true;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (playerController != null) playerController.movementDisabled = false;

        _exitingCar = false;

        // Start post-exit sequence (turn around then close door)
        if (debugLogs) Debug.Log("✅ Exit complete, now turning to close door...");
        StartCoroutine(PostExitSequence());
    }

    private IEnumerator PostExitSequence()
    {
        // First, turn the character back toward the car to close the door naturally
        yield return StartCoroutine(TurnTowardsCar());
        
        // Then close the door
        yield return StartCoroutine(PostExitDoorClose());
    }

    private IEnumerator TurnTowardsCar()
    {
        if (debugLogs) Debug.Log("Turning character toward car...");

        // Calculate the direction toward the door/car
        Vector3 doorDirection = (Door.position - player.position).normalized;
        doorDirection.y = 0; // Keep rotation horizontal only
        Quaternion targetRotation = Quaternion.LookRotation(doorDirection);

        Quaternion startRotation = player.rotation;
        float turnDuration = 0.6f; // Adjust for faster/slower turning
        float elapsed = 0f;

        // Temporarily disable player movement during turn
        var playerController = player.GetComponent<PlayerController>();
        if (playerController != null) playerController.movementDisabled = true;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / turnDuration);
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing

            player.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        // Ensure final rotation is exact
        player.rotation = GetYawOnlyRotation(targetRotation, player.rotation);

        // Re-enable movement after turning
        if (playerController != null) playerController.movementDisabled = false;

        if (debugLogs) Debug.Log("✅ Character turned toward car, ready to close door!");
    }

    private IEnumerator PostExitDoorClose()
    {
        if (debugLogs) Debug.Log("Starting post-exit door closing animation...");

        // Use the dedicated post-exit hand target if available, otherwise fallback to door handle
        Transform targetHandle = PostExitHandTarget != null ? PostExitHandTarget : DoorHandle;

        if (PreAnimLeftIK == null || PreAnimLeftTarget == null || targetHandle == null || Door == null)
        {
            Debug.LogWarning("Post-exit door close components not assigned! Skipping animation.");
            yield break;
        }

        // Store starting values
        Vector3 startPos = PreAnimLeftTarget.position;
        Vector3 handlePos = targetHandle.position;

        // Door rotation values (currently open, want to close)
        Quaternion doorOpenRot = Door.localRotation; // Current open position
        Quaternion doorClosedRot = _doorClosedLocalRotation; // Closed position (cached)
        bool closeSoundPlayed = false;

        float elapsed = 0f;

        // Phase 1: Reach for post-exit target position from standing position
        float reachDuration = 0.5f;
        
        // Calculate natural reach from outside position
        Vector3 playerPos = player.position;
        Vector3 reachDirection = (handlePos - playerPos).normalized;
        
        // Create smooth arc motion to target position from outside
        Vector3 midReachPos = Vector3.Lerp(startPos, handlePos, 0.5f);
        midReachPos += player.up * 0.1f; // Slight upward arc
        midReachPos += reachDirection * 0.1f; // Natural reach motion

        while (elapsed < reachDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / reachDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentPos;
            if (t < 0.5f)
            {
                currentPos = Vector3.Lerp(startPos, midReachPos, t * 2f);
            }
            else
            {
                currentPos = Vector3.Lerp(midReachPos, handlePos, (t - 0.5f) * 2f);
            }

            PreAnimLeftTarget.position = currentPos;
            PreAnimLeftIK.weight = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // Ensure hand is at target position
        PreAnimLeftTarget.position = handlePos;
        PreAnimLeftIK.weight = 1f;

        // Phase 2: Pull door closed while following the target (if it moves during door closing)
        float closeDuration = 0.7f;
        elapsed = 0f;

        // Calculate pull motion based on target position
        Vector3 startPullPos = handlePos;
        Vector3 playerToDoor = (Door.position - playerPos).normalized;
        
        // Create natural pulling motion - the target should be positioned to avoid door collision
        Vector3 pullDirection = playerToDoor * 0.3f; // Pull slightly toward car
        Vector3 pullEndPos = startPullPos + pullDirection;
        pullEndPos.y -= 0.05f; // Slight downward motion while pulling

        // Mid-point for natural pulling arc
        Vector3 midPullPos = Vector3.Lerp(startPullPos, pullEndPos, 0.5f);
        midPullPos += player.right * -0.1f; // Adjust based on door swing direction
        midPullPos.y += 0.02f; // Slight lift during pull

        while (elapsed < closeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / closeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            // If PostExitHandTarget is assigned, follow its position (in case you animate the target itself)
            Vector3 currentTargetPos = PostExitHandTarget != null ? PostExitHandTarget.position : handlePos;
            
            // Smooth arc motion for pulling door closed
            Vector3 currentPos;
            if (t < 0.5f)
            {
                currentPos = Vector3.Lerp(startPullPos, midPullPos, t * 2f);
            }
            else
            {
                currentPos = Vector3.Lerp(midPullPos, pullEndPos, (t - 0.5f) * 2f);
            }

            // If using a specific target, blend between calculated motion and target position
            if (PostExitHandTarget != null)
            {
                currentPos = Vector3.Lerp(currentPos, currentTargetPos, 0.7f); // 70% target, 30% calculated motion
            }

            PreAnimLeftTarget.position = currentPos;
            
            // Close door with smooth motion
            Door.localRotation = Quaternion.Slerp(doorOpenRot, doorClosedRot, t);
            if (!closeSoundPlayed && Quaternion.Angle(Door.localRotation, doorClosedRot) <= doorCloseSoundAngleThreshold)
            {
                PlayDoorSound(carDoorCloseClip);
                closeSoundPlayed = true;
            }

            yield return null;
        }

        // Phase 3: Brief settle and natural release
        yield return new WaitForSeconds(0.1f);

        // Phase 4: Return hand to natural resting position
        float releaseDuration = 0.4f;
        elapsed = 0f;
        Vector3 releaseStartPos = PreAnimLeftTarget.position;
        Vector3 naturalRestPos = playerPos + player.forward * 0.3f + player.up * 0.8f; // Natural arm position while standing

        while (elapsed < releaseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / releaseDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            PreAnimLeftTarget.position = Vector3.Lerp(releaseStartPos, naturalRestPos, t);
            // Gradually reduce IK weight to return to natural pose
            PreAnimLeftIK.weight = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        // Ensure IK is fully disabled
        PreAnimLeftIK.weight = 0f;

        if (debugLogs) Debug.Log("✅ Post-exit door closing animation complete!");
    }

    // ------------------- Scene Visualization -------------------
    private void OnDrawGizmosSelected()
    {
        if (enterAreaCollider == null) return;

        Gizmos.color = Color.green;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(enterAreaCollider.transform.position, enterAreaCollider.transform.rotation, enterAreaCollider.transform.lossyScale);
        Gizmos.DrawWireCube(enterAreaCollider.center, enterAreaCollider.size);
        Gizmos.matrix = old;
    }

    private void EnsureAudioSource()
    {
        if (carDoorAudioSource != null) return;
        carDoorAudioSource = gameObject.AddComponent<AudioSource>();
        carDoorAudioSource.playOnAwake = false;
        carDoorAudioSource.loop = false;
        carDoorAudioSource.spatialBlend = 1f;
    }

    private void PlayDoorSound(AudioClip clip)
    {
        if (clip == null || carDoorAudioSource == null) return;
        carDoorAudioSource.pitch = carDoorPitch;
        carDoorAudioSource.volume = carDoorVolume;
        carDoorAudioSource.PlayOneShot(clip);
    }

    private Quaternion GetYawOnlyRotation(Quaternion targetRotation, Quaternion fallbackRotation)
    {
        Vector3 flattenedForward = Vector3.ProjectOnPlane(targetRotation * Vector3.forward, Vector3.up);
        if (flattenedForward.sqrMagnitude < 0.0001f)
            flattenedForward = Vector3.ProjectOnPlane(fallbackRotation * Vector3.forward, Vector3.up);
        if (flattenedForward.sqrMagnitude < 0.0001f)
            flattenedForward = Vector3.forward;

        return Quaternion.LookRotation(flattenedForward.normalized, Vector3.up);
    }
}
