using UnityEngine;

public class SmoothCameraZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float zoomedFOV = 30f;
    [SerializeField] private float zoomSpeed = 2f;
    
    [Header("Input Settings")]
    [SerializeField] private KeyCode zoomKey = KeyCode.Q;
    
    private Camera cam;
    private float targetFOV;
    
    void Start()
    {
        // Get the camera component
        cam = GetComponent<Camera>();
        
        // If no camera is found on this GameObject, try to get the main camera
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("No camera found! Please attach this script to a GameObject with a Camera component.");
                enabled = false;
                return;
            }
        }
        
        // Set initial values
        defaultFOV = cam.fieldOfView;
        targetFOV = defaultFOV;
    }
    
    void Update()
    {
        HandleZoomInput();
        UpdateCameraZoom();
    }
    
    void HandleZoomInput()
    {
        if (Input.GetKey(zoomKey))
        {
            // Zoom in when Q is held
            targetFOV = zoomedFOV;
        }
        else
        {
            // Zoom out when Q is released
            targetFOV = defaultFOV;
        }
    }
    
    void UpdateCameraZoom()
    {
        // Smoothly interpolate the camera's field of view
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
    }
    
    // Optional: Method to set zoom values via code
    public void SetZoomValues(float defaultFov, float zoomedFov, float speed)
    {
        defaultFOV = defaultFov;
        zoomedFOV = zoomedFov;
        zoomSpeed = speed;
        targetFOV = defaultFOV;
    }
    
    // Optional: Method to change zoom key via code
    public void SetZoomKey(KeyCode newKey)
    {
        zoomKey = newKey;
    }
    
    // Optional: Method to instantly set zoom without smooth transition
    public void SetInstantZoom(bool isZoomed)
    {
        targetFOV = isZoomed ? zoomedFOV : defaultFOV;
        cam.fieldOfView = targetFOV;
    }
}