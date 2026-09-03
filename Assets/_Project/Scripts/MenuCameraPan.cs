using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MenuCameraPan : MonoBehaviour
{
    public float panAmount = 5f;     // How much the camera tilts (degrees)
    public float smoothSpeed = 5f;   // How smooth the camera follows
    public bool useRelativeInput = true; // Use mouse delta when cursor is hidden/locked
    public float relativeSensitivity = 0.003f; // Tuning for delta to normalized range

    private Quaternion defaultRotation;
    private Vector2 virtualPos; // Normalized (-1..1) virtual cursor

    void Start()
    {
        defaultRotation = transform.rotation;
        virtualPos = Vector2.zero;
    }

    void Update()
    {
        // Get mouse position normalized (-1 to 1) for either input system.
        Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
            mouseDelta = Mouse.current.delta.ReadValue();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        mousePos = Input.mousePosition;
#endif

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        bool cursorHiddenOrLocked = Cursor.lockState == CursorLockMode.Locked || !Cursor.visible;
        if (useRelativeInput || cursorHiddenOrLocked)
        {
            // Integrate delta into a virtual cursor so it works without a visible cursor.
            virtualPos += mouseDelta * relativeSensitivity;
            virtualPos.x = Mathf.Clamp(virtualPos.x, -1f, 1f);
            virtualPos.y = Mathf.Clamp(virtualPos.y, -1f, 1f);
            mousePos = new Vector2(
                (virtualPos.x * 0.5f + 0.5f) * Screen.width,
                (virtualPos.y * 0.5f + 0.5f) * Screen.height
            );
        }

        float mouseX = (mousePos.x / Screen.width - 0.5f) * 2f;
        float mouseY = (mousePos.y / Screen.height - 0.5f) * 2f;

        // Calculate target rotation
        Quaternion targetRotation = defaultRotation * 
            Quaternion.Euler(-mouseY * panAmount, mouseX * panAmount, 0);

        // Smoothly move camera towards target
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}
