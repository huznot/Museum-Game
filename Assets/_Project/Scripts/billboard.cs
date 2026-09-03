using UnityEngine;

public class Billboard : MonoBehaviour
{
    public enum BillboardType
    {
        LookAtCamera,
        CameraForward
    }

    [Header("Billboard Mode")]
    [SerializeField] private BillboardType billboardType = BillboardType.LookAtCamera;

    [Header("Lock Rotation")]
    [SerializeField] private bool lockX;
    [SerializeField] private bool lockY;
    [SerializeField] private bool lockZ;

    [Header("Fix Backwards")]
    [SerializeField] private bool flip180 = true; // turn this on

    private Vector3 originalRotation;
    private Camera cam;

    private void Awake()
    {
        originalRotation = transform.rotation.eulerAngles;
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (!cam) return;

        // Billboard logic
        switch (billboardType)
        {
            case BillboardType.LookAtCamera:
                transform.LookAt(cam.transform.position, Vector3.up);
                break;

            case BillboardType.CameraForward:
                transform.forward = cam.transform.forward;
                break;
        }

        // Lock axes
        Vector3 rotation = transform.rotation.eulerAngles;

        if (lockX) rotation.x = originalRotation.x;
        if (lockY) rotation.y = originalRotation.y;
        if (lockZ) rotation.z = originalRotation.z;

        transform.rotation = Quaternion.Euler(rotation);

        // Fix backwards-facing quads/sprites
        if (flip180)
        {
            transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
