using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityTutorial.Manager;
using UnityTutorial.PlayerControl;

[RequireComponent(typeof(Animator))]
public class SitInCarController : MonoBehaviour
{
    public bool isSitting = false;
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    
    [Header("Car Setup - IMPORTANT!")]
    public GameObject carGameObject; 
    public Transform driverSeat;    
    private CarController carController;
    private Quaternion currentLegTargetRotation;

    

    [Header("Leg Bones")]
    public Transform leftThigh;
    public Transform rightThigh;
    public Transform leftCalf;
    public Transform rightCalf;
    public Transform rightFoot;

    [Header("Sitting Leg Rotations")]
    public Vector3 leftThighRotation = new Vector3(280.626465f, 32.2001457f, 152.046982f);
    public Vector3 rightThighRotation = new Vector3(288.03952f, 343.230225f, 195.832977f);
    public Vector3 leftCalfRotation = new Vector3(283.589325f, 9.4531374f, 2.27479339f);
    public Vector3 rightCalfRotation = new Vector3(285.976746f, 338.601929f, 27.7363052f);
    
    [Header("Accelerator Foot Movement")]
    public Vector3 rightCalfPressedRotation = new Vector3(270f, 338.601929f, 27.7363052f); // Extended forward rotation (adjust X to extend more)
    public float footMoveSpeed = 8f; // Speed of foot movement

    [Header("Handbrake Movement")]
    public Vector3 rightHandDefaultLocalPos = new Vector3(0.00132499996f, 0.000709999993f, -0.00115999999f);
    public Vector3 rightHandBrakeLocalPos = new Vector3(0.00321f, -0.00264000008f, -0.00418000016f);
    public float handMoveSpeed = 5f;
    [Header("Handbrake Audio")]
    public AudioClip handbrakeClip;
    public AudioSource handbrakeAudioSource;
    [Range(0f, 1f)] public float handbrakeVolume = 0.9f;
    public float handbrakePitch = 1f;

    [Header("Steering Wheel")]
    public Transform steeringWheel;
    public float maxSteerAngle = 45f;
    public float steerSpeed = 120f;

    // Add references for the IK constraints
    [Header("Rig Constraints")]
    public TwoBoneIKConstraint LeftArmIk;
    public TwoBoneIKConstraint RightArmIk;

    private Animator animator;
    private PlayerController playerController;
    private Rigidbody playerRigidbody;
    
    // Store original state
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private bool originalKinematic;
    private bool originalUseGravity;
    private bool originalDetectCollisions;

    // Cache default rotations
    private Quaternion leftThighDefaultRot;
    private Quaternion rightThighDefaultRot;
    private Quaternion leftCalfDefaultRot;
    private Quaternion rightCalfDefaultRot;
    private Quaternion rightCalfPressedRot;
    private bool _defaultLegRotationsCached = false;

    private float currentSteerAngle = 0f;

    [Header("Head Control")]
    public Transform headBone;
    public Transform cameraTransform;
    public Transform cameraRoot; 
    public float mouseSensitivity = 70f;
    [Header("Head Rotation Limits")]
    public float headHorizontalLimit = 120f; // How far head can turn left/right
    public float headVerticalLimit = 45f; // How far head can look up/down
    [HideInInspector] public float currentHeadRotX = 0f;
    [HideInInspector] public float currentHeadRotY = 0f;
    [HideInInspector] public Quaternion headDefaultRot;
    private bool _headDefaultCached = false;
    
    private float _xRotation;
    private float _sittingYRotation = 0f;
    private float _headXRotation = 0f;
    private float _headYRotation = 0f;

    private Transform rightHandOriginalParent;
    private bool isUsingHandbrake = false;
    private bool _handbrakeSoundPlayed = false;
    [Header("Sit Position Reference")]
    public Transform SitPos; 
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 5f;
    [Header("Seat Final Lock")]
    [SerializeField] private float finalLockDistance = 0.015f;
    [SerializeField] private float finalLockAngle = 0.75f;

    private void Awake()
    {
        if (headBone != null)
        {
            headDefaultRot = headBone.localRotation;
            _headDefaultCached = true;
        }

        CacheDefaultLegRotations();
    }

    public void OnEnable()
    {
        if (!isSitting) return;

        // Reset look offsets each time we sit so the camera/head start centered instead of using stale values
        _xRotation = 0f;
        _sittingYRotation = 0f;
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
            if (cameraRoot != null)
                cameraTransform.position = cameraRoot.position;
        }

        if (headBone != null)
        {
            if (!_headDefaultCached)
            {
                headDefaultRot = headBone.localRotation;
                _headDefaultCached = true;
            }
            else
            {
                headBone.localRotation = headDefaultRot;
            }
        }
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false; // Disable root motion

        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerRigidbody = GetComponent<Rigidbody>();

        if (carGameObject != null)
        {
            carController = carGameObject.GetComponent<CarController>();
            if (carController == null)
            {
            }

            // Parent the character to the car object at start
            transform.SetParent(carGameObject.transform, true);
        }
        else
        {
        }

        // Set Rigidbody properties
        playerRigidbody.isKinematic = true;




        // Store original state
        originalParent = transform.parent;
        originalKinematic = playerRigidbody.isKinematic;
        originalUseGravity = playerRigidbody.useGravity;
        originalDetectCollisions = playerRigidbody.detectCollisions;

        CacheDefaultLegRotations();
        rightCalfPressedRot = Quaternion.Euler(rightCalfPressedRotation);

        // Store the right hand's original parent (should be steering wheel)
        if (rightHandTarget != null)
        {
            rightHandOriginalParent = rightHandTarget.parent;
            rightHandTarget.localPosition = rightHandDefaultLocalPos;
        }

        // Set TwoBoneIKConstraint weights to 1
        if (LeftArmIk != null) LeftArmIk.weight = 1f;
        if (RightArmIk != null) RightArmIk.weight = 1f;
        carController.enabled = true;
        carController.SetSitInCarController(this);

        currentLegTargetRotation = Quaternion.Euler(rightCalfRotation); 
        EnsureHandbrakeAudioSource();

    }

    private void OnDisable()
    {
        // Restore head/camera so re-entering starts centered
        if (headBone != null && _headDefaultCached)
            headBone.localRotation = headDefaultRot;

        if (leftThigh != null) leftThigh.localRotation = leftThighDefaultRot;
        if (rightThigh != null) rightThigh.localRotation = rightThighDefaultRot;
        if (leftCalf != null) leftCalf.localRotation = leftCalfDefaultRot;
        if (rightCalf != null) rightCalf.localRotation = rightCalfDefaultRot;

        _xRotation = 0f;
        _sittingYRotation = 0f;
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
            if (cameraRoot != null)
                cameraTransform.position = cameraRoot.position;
        }
    }
  
    public void SitInCar()
    {
        // In SitInCarController.cs, inside SitInCar()

        carController.SetSitInCarController(this);

        if (carGameObject == null)
        {
            return;
        }
        
        isSitting = true;
        
        // Reset camera rotation values when starting to sit
        _xRotation = 0f;
        _sittingYRotation = 0f;
        
        // Initialize right calf rotation to default sitting position
        if (rightCalf != null)
        {
            rightCalf.localRotation = Quaternion.Euler(rightCalfRotation);
        }
        
        // DISABLE RIGIDBODY COMPLETELY FIRST
        playerRigidbody.isKinematic = true;
        playerRigidbody.useGravity = false;
        playerRigidbody.detectCollisions = false;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        carController.enabled = true;

        // Parent to car BEFORE positioning
        transform.SetParent(carGameObject.transform, true);
        // Reset car velocity when player sits
        if (carController != null && carController.TryGetComponent<Rigidbody>(out var carRb))
        {
            carRb.linearVelocity = Vector3.zero;
            carRb.angularVelocity = Vector3.zero;
        }

        
        

        
    }
    

    void Update()
    {
        if (!isSitting) return;
        if (isSitting)
        {
            if (SitPos == null) return;

            // Smoothly move towards position
            transform.position = Vector3.Lerp(
                transform.position,
                SitPos.position,
                Time.deltaTime * moveSpeed
            );

            // Smoothly rotate towards target rotation
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                SitPos.rotation,
                Time.deltaTime * rotateSpeed
            );

            // Keep smooth entry, then hard-lock only at the very end so pose is deterministic.
            if (Vector3.Distance(transform.position, SitPos.position) <= finalLockDistance &&
                Quaternion.Angle(transform.rotation, SitPos.rotation) <= finalLockAngle)
            {
                transform.position = SitPos.position;
                transform.rotation = SitPos.rotation;
            }
            playerRigidbody.interpolation = RigidbodyInterpolation.None;

        }

        if (playerController != null)
            playerController.movementDisabled = isSitting;

        animator.SetBool("IsSitting", isSitting);


        if (isSitting)
        {
            
            animator.Play("Empty");

            // Handle camera movement when sitting
            HandleSittingCameraMovement();

            // Handbrake with proper parenting
            bool shouldUseHandbrake = Input.GetKey(KeyCode.Space);
            
            if (rightHandTarget != null)
            {
                // Handle parenting changes
                if (shouldUseHandbrake && !isUsingHandbrake)
                {
                    // Starting to use handbrake - unparent from steering wheel
                    Vector3 worldPos = rightHandTarget.position;
                    Quaternion worldRot = rightHandTarget.rotation;
                    
                    rightHandTarget.SetParent(carGameObject.transform, true);
                    
                    rightHandTarget.position = worldPos;
                    rightHandTarget.rotation = worldRot;
                    isUsingHandbrake = true;
                    if (!_handbrakeSoundPlayed)
                    {
                        PlayHandbrakeSound();
                        _handbrakeSoundPlayed = true;
                    }
                }
                else if (!shouldUseHandbrake && isUsingHandbrake)
                {
                    // Stopping handbrake use - reparent to steering wheel
                    Vector3 worldPos = rightHandTarget.position;
                    Quaternion worldRot = rightHandTarget.rotation;
                    
                    rightHandTarget.SetParent(rightHandOriginalParent, true);
                    
                    rightHandTarget.position = worldPos;
                    rightHandTarget.rotation = worldRot;
                    isUsingHandbrake = false;
                    _handbrakeSoundPlayed = false;
                }

                // Move hand to target position
                Vector3 targetLocalPos = shouldUseHandbrake
                    ? rightHandBrakeLocalPos
                    : rightHandDefaultLocalPos;

                rightHandTarget.localPosition = Vector3.Lerp(
                    rightHandTarget.localPosition,
                    targetLocalPos,
                    Time.deltaTime * handMoveSpeed
                );
            }

            // Steering wheel visual
            float steerInput = 0f;
            if (Input.GetKey(KeyCode.A)) steerInput = -1f;
            else if (Input.GetKey(KeyCode.D)) steerInput = 1f;

            if (steerInput != 0f)
            {
                currentSteerAngle += steerInput * steerSpeed * Time.deltaTime;
                currentSteerAngle = Mathf.Clamp(currentSteerAngle, -maxSteerAngle, maxSteerAngle);
            }
            else
            {
                currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, 0f, steerSpeed * Time.deltaTime);
            }

            if (steeringWheel != null)
            {
                steeringWheel.localRotation = Quaternion.Euler(0f, 0f, -currentSteerAngle);
            }
            
            // If accelerating → target = pressed rotation
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
            {
                Debug.Log("Accelerating - moving foot");
                currentLegTargetRotation = Quaternion.Euler(rightCalfPressedRotation);
            }
            else
            {
                // If not accelerating → target = default rotation
                currentLegTargetRotation = Quaternion.Euler(rightCalfRotation);
            }

            // Always rotate smoothly toward the target
            rightCalf.localRotation = Quaternion.Slerp(
                rightCalf.localRotation,
                currentLegTargetRotation,
                Time.deltaTime * footMoveSpeed
            );
        }
    }

    void LateUpdate()
    {
        if (!isSitting) return;

        if (isSitting)
        {
            // Head follows camera rotation (camera controls, head matches)
            if (headBone != null && cameraTransform != null)
            {
                // Get the camera's current rotation and apply it to the head
                headBone.rotation = cameraTransform.rotation;
            }

            // Set leg rotations
            if (leftThigh != null) leftThigh.localRotation = Quaternion.Euler(leftThighRotation);
            if (rightThigh != null) rightThigh.localRotation = Quaternion.Euler(rightThighRotation);
            if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(leftCalfRotation);
            
        }
        else
        {
            // Restore default rotations when not sitting
            if (headBone != null) headBone.localRotation = headDefaultRot;
            if (leftThigh != null) leftThigh.localRotation = leftThighDefaultRot;
            if (rightThigh != null) rightThigh.localRotation = rightThighDefaultRot;
            if (leftCalf != null) leftCalf.localRotation = leftCalfDefaultRot;
            if (rightCalf != null) rightCalf.localRotation = rightCalfDefaultRot;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!isSitting) return;

        if (isSitting)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
        }
    }

    private void HandleSittingCameraMovement()
    {
        if (cameraTransform == null || cameraRoot == null) return;

        // Get mouse input from the same InputManager that PlayerController uses
        InputManager inputManager = GetComponent<InputManager>();
        if (inputManager == null) return;

        float mouseX = inputManager.Look.x;
        float mouseY = inputManager.Look.y;

        // Update camera position to camera root
        cameraTransform.position = cameraRoot.position;

        // Update camera rotation with LARGER range for looking around the car
        _xRotation -= mouseY * mouseSensitivity * Time.smoothDeltaTime;
        _xRotation = Mathf.Clamp(_xRotation, -headVerticalLimit, headVerticalLimit);

        _sittingYRotation += mouseX * mouseSensitivity * Time.smoothDeltaTime;
        _sittingYRotation = Mathf.Clamp(_sittingYRotation, -headHorizontalLimit, headHorizontalLimit);

        // Apply rotation to camera - camera is the master, head will follow
        cameraTransform.localRotation = Quaternion.Euler(_xRotation, _sittingYRotation, 0);
    }

    private void EnsureHandbrakeAudioSource()
    {
        if (handbrakeAudioSource != null) return;
        handbrakeAudioSource = gameObject.AddComponent<AudioSource>();
        handbrakeAudioSource.playOnAwake = false;
        handbrakeAudioSource.loop = false;
        handbrakeAudioSource.spatialBlend = 1f;
    }

    private void PlayHandbrakeSound()
    {
        if (handbrakeAudioSource == null || handbrakeClip == null) return;
        handbrakeAudioSource.pitch = handbrakePitch;
        handbrakeAudioSource.volume = handbrakeVolume;
        handbrakeAudioSource.PlayOneShot(handbrakeClip);
    }

    private void CacheDefaultLegRotations()
    {
        if (_defaultLegRotationsCached) return;

        if (leftThigh != null) leftThighDefaultRot = leftThigh.localRotation;
        if (rightThigh != null) rightThighDefaultRot = rightThigh.localRotation;
        if (leftCalf != null) leftCalfDefaultRot = leftCalf.localRotation;
        if (rightCalf != null) rightCalfDefaultRot = rightCalf.localRotation;

        _defaultLegRotationsCached = true;
    }
}
