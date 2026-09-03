using UnityEngine;

public class FirstPersonHandIK : MonoBehaviour
{
    [Header("IK Settings")]
    public Transform cameraTransform;
    public float handReachDistance = 1.5f;
    public float handOffsetFromCenter = 0.3f;
    public float handForwardOffset = 0.5f;
    public float ikWeight = 1f;
    public float transitionSpeed = 5f;
    
    [Header("Hand Positions")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    
    private Animator animator;
    private bool isRightHandActive = false;
    private bool isLeftHandActive = false;
    private float currentRightIKWeight = 0f;
    private float currentLeftIKWeight = 0f;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        
        // Create hand target objects if they don't exist
        if (leftHandTarget == null)
        {
            GameObject leftTarget = new GameObject("LeftHandTarget");
            leftHandTarget = leftTarget.transform;
        }
        
        if (rightHandTarget == null)
        {
            GameObject rightTarget = new GameObject("RightHandTarget");
            rightHandTarget = rightTarget.transform;
        }
        
        // If no camera assigned, try to find the main camera
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
        }
    }
    
    void Update()
    {
        HandleInput();
        UpdateHandTargets();
        UpdateIKWeights();
    }
    
    void HandleInput()
    {
        // Right mouse button for right hand
        if (Input.GetMouseButtonDown(1))
        {
            isRightHandActive = true;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isRightHandActive = false;
        }
        
        // Left mouse button for left hand
        if (Input.GetMouseButtonDown(0))
        {
            isLeftHandActive = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isLeftHandActive = false;
        }
    }
    
    void UpdateHandTargets()
    {
        if (cameraTransform == null) return;
        
        // Calculate base position in front of camera
        Vector3 basePosition = cameraTransform.position + cameraTransform.forward * handForwardOffset;
        
        // Update right hand target
        if (isRightHandActive)
        {
            Vector3 rightHandPos = basePosition + cameraTransform.right * handOffsetFromCenter;
            rightHandPos += cameraTransform.forward * handReachDistance;
            rightHandTarget.position = rightHandPos;
            rightHandTarget.rotation = cameraTransform.rotation;
        }
        
        // Update left hand target
        if (isLeftHandActive)
        {
            Vector3 leftHandPos = basePosition - cameraTransform.right * handOffsetFromCenter;
            leftHandPos += cameraTransform.forward * handReachDistance;
            leftHandTarget.position = leftHandPos;
            leftHandTarget.rotation = cameraTransform.rotation;
        }
    }
    
    void UpdateIKWeights()
    {
        // Smooth transition for IK weights
        float targetRightWeight = isRightHandActive ? ikWeight : 0f;
        float targetLeftWeight = isLeftHandActive ? ikWeight : 0f;
        
        currentRightIKWeight = Mathf.Lerp(currentRightIKWeight, targetRightWeight, transitionSpeed * Time.deltaTime);
        currentLeftIKWeight = Mathf.Lerp(currentLeftIKWeight, targetLeftWeight, transitionSpeed * Time.deltaTime);
    }
    
    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;
        
        // Set right hand IK
        if (rightHandTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, currentRightIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, currentRightIKWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }
        
        // Set left hand IK
        if (leftHandTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentLeftIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentLeftIKWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        }
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (cameraTransform == null) return;
        
        // Draw hand target positions
        Gizmos.color = Color.red;
        if (isRightHandActive && rightHandTarget != null)
        {
            Gizmos.DrawWireSphere(rightHandTarget.position, 0.05f);
            Gizmos.DrawLine(cameraTransform.position, rightHandTarget.position);
        }
        
        Gizmos.color = Color.blue;
        if (isLeftHandActive && leftHandTarget != null)
        {
            Gizmos.DrawWireSphere(leftHandTarget.position, 0.05f);
            Gizmos.DrawLine(cameraTransform.position, leftHandTarget.position);
        }
    }
}