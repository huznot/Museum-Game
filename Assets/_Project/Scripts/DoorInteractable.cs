using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CarDoorGrabbable : MonoBehaviour
{
    [Header("Door Settings")]
    public Transform doorHandle;         // The handle position for hand targeting
    // Door pivots around this object's transform
    public Vector3 rotationAxis = Vector3.up; // Which axis to rotate around (Y is default for car doors)
    public float maxOpenAngle = 70f;     // Maximum door opening angle
    public float doorSpeed = 50f;        // Degrees per second when opening/closing
    
    [Header("Interaction")]
    public float grabDistance = 1.5f;    // Max distance to grab handle
    public LayerMask handLayer = -1;     // What layers can grab (optional)
    
    [Header("Visual Feedback")]
    public GameObject grabPrompt;        // UI element showing "Press to grab"
    
    private float currentDoorAngle = 0f;
    private Quaternion initialRotation; // Store the door's starting rotation
    private bool isGrabbed = false;
    private bool isRightHandGrabbing = false;
    private ArmMouseIKRig2 handController;
    private Transform activeHand;
    private Vector3 originalHandTarget;
    
    void Start()
    {
        handController = FindObjectOfType<ArmMouseIKRig2>();
        if (grabPrompt) grabPrompt.SetActive(false);
        
        // Store the initial rotation so we can rotate relative to it
        initialRotation = transform.rotation;
        
        Debug.Log($"Door initial rotation: {initialRotation.eulerAngles}");
        Debug.Log($"Door rotation axis set to: {rotationAxis}");
    }
    
    void Update()
    {
        CheckForHandsNearby();
        
        if (isGrabbed)
        {
            HandleDoorControl();
            CheckForRelease();
        }
    }
    
    void CheckForHandsNearby()
    {
        if (isGrabbed || !handController) return;
        
        bool rightHandNear = IsHandNearHandle(handController.rightTarget);
        bool leftHandNear = IsHandNearHandle(handController.leftTarget);
        
        // Show grab prompt if either hand is nearby
        if (grabPrompt)
        {
            grabPrompt.SetActive(rightHandNear || leftHandNear);
        }
        
        // Check for grab input
        if (rightHandNear && Input.GetMouseButtonDown(1))
        {
            GrabHandle(true);
        }
        else if (leftHandNear && Input.GetMouseButtonDown(0))
        {
            GrabHandle(false);
        }
    }
    
    bool IsHandNearHandle(Transform handTarget)
    {
        if (!handTarget || !doorHandle) return false;
        
        float distance = Vector3.Distance(handTarget.position, doorHandle.position);
        return distance <= grabDistance;
    }
    
    void GrabHandle(bool isRightHand)
    {
        isGrabbed = true;
        isRightHandGrabbing = isRightHand;
        activeHand = isRightHand ? handController.rightTarget : handController.leftTarget;
        
        // Store original position for reach limit checking
        originalHandTarget = activeHand.position;
        
        // Lock hand to handle position
        activeHand.position = doorHandle.position;
        
        // Tell the hand controller this hand is grabbed
        handController.SetGrabbed(isRightHand, true);
        
        if (grabPrompt) grabPrompt.SetActive(false);
        
        Debug.Log($"{(isRightHand ? "Right" : "Left")} hand grabbed door handle");
    }
    
    void HandleDoorControl()
    {
        // Get scroll wheel input
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Calculate desired angle change
            float angleChange = scroll * doorSpeed * Time.deltaTime * 10f; // Multiply for responsiveness
            float newAngle = currentDoorAngle + angleChange;
            
            // Clamp to valid range
            newAngle = Mathf.Clamp(newAngle, 0f, maxOpenAngle);
            
            // Check if hand can reach the new handle position
            if (CanHandReachNewPosition(newAngle))
            {
                currentDoorAngle = newAngle;
                UpdateDoorPosition();
            }
        }
    }
    
    bool CanHandReachNewPosition(float newAngle)
    {
        // Calculate where the handle would be at the new angle
        Vector3 handleLocalPos = transform.InverseTransformPoint(doorHandle.position);
        Quaternion additionalRotation = Quaternion.AngleAxis(newAngle, rotationAxis);
        Vector3 newHandleWorldPos = transform.position + (transform.rotation * additionalRotation * handleLocalPos);
        
        // Check if the hand can reach this position from its anchor
        Transform anchor = isRightHandGrabbing ? handController.rightAnchor : handController.leftAnchor;
        float distanceFromAnchor = Vector3.Distance(anchor.position, newHandleWorldPos);
        
        return distanceFromAnchor <= handController.radius;
    }
    
    void UpdateDoorPosition()
    {
        // Rotate the door around its rotation axis from the initial rotation
        Quaternion additionalRotation = Quaternion.AngleAxis(currentDoorAngle, rotationAxis);
        transform.rotation = initialRotation * additionalRotation;
        
        // Update hand position to follow handle
        if (activeHand && doorHandle)
        {
            activeHand.position = doorHandle.position;
        }
        
        // Debug info
        if (Time.frameCount % 30 == 0) // Every 30 frames to avoid spam
        {
            Debug.Log($"Door angle: {currentDoorAngle:F1}°, Handle position: {doorHandle.position}");
        }
    }
    
    void CheckForRelease()
    {
        bool releaseInput = isRightHandGrabbing ? 
            Input.GetMouseButtonUp(1) : 
            Input.GetMouseButtonUp(0);
            
        if (releaseInput)
        {
            ReleaseHandle();
        }
    }
    
    void ReleaseHandle()
    {
        // Tell hand controller this hand is no longer grabbed
        handController.SetGrabbed(isRightHandGrabbing, false);
        
        isGrabbed = false;
        activeHand = null;
        
        Debug.Log("Released door handle");
    }
    
    // Optional: Visual debug in scene view
    void OnDrawGizmosSelected()
    {
        if (doorHandle)
        {
            // Draw grab radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(doorHandle.position, grabDistance);
            
            // Draw door arc
            Gizmos.color = Color.blue;
            
            // Draw max opening arc using the specified rotation axis
            for (int i = 0; i <= 20; i++)
            {
                float angle = (maxOpenAngle / 20f) * i;
                Quaternion rotation = Quaternion.AngleAxis(angle, rotationAxis);
                Vector3 direction = rotation * transform.forward;
                Gizmos.DrawRay(transform.position, direction * 2f);
            }
        }
    }
}