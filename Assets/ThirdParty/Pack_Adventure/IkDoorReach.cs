using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DoorOpener : MonoBehaviour
{
    public Transform player;
    public Transform playerCamera;    // assign your first-person camera
    public Transform door;
    public Transform doorHandle;
    public Transform ikTarget;
    public Rig rigLayer;
    public Transform seatTarget;      // position inside car to look at

    public float activationDistance = 2.5f;
    public float openSpeed = 50f;
    public float targetOpenAngle = 90f;
    public float ikMoveSpeed = 5f;
    public float delayBeforeOpen = 0.5f;
    public float cameraRotateSpeed = 3f;

    private bool isGrabbing = false;
    private bool handOnHandle = false;
    private bool isDoorOpening = false;
    private bool doorOpened = false;

    private float grabStartTime = 0f;

    private Quaternion initialCamRotation;
    private Quaternion lookDownRotation;
    private Quaternion lookUpRotation;

    void Start()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Assign playerCamera in inspector!");
            enabled = false;
            return;
        }

        // Save initial camera rotation
        initialCamRotation = playerCamera.rotation;

        // Calculate lookDownRotation (look at door handle)
        Vector3 dirToHandle = (doorHandle.position - playerCamera.position).normalized;
        lookDownRotation = Quaternion.LookRotation(dirToHandle);

        // Calculate lookUpRotation (look at seat inside car)
        Vector3 dirToSeat = (seatTarget.position - playerCamera.position).normalized;
        lookUpRotation = Quaternion.LookRotation(dirToSeat);
    }

    void Update()
    {
        float dist = Vector3.Distance(player.position, doorHandle.position);

        if (dist < activationDistance && Input.GetKeyDown(KeyCode.E) && !doorOpened)
        {
            isGrabbing = true;
            handOnHandle = false;
            isDoorOpening = false;
            grabStartTime = Time.time;
        }

        if (dist >= activationDistance)
        {
            isGrabbing = false;
        }

        rigLayer.weight = Mathf.Lerp(rigLayer.weight, isGrabbing ? 1f : 0f, Time.deltaTime * 6f);

        if (isGrabbing)
        {
            // Step 1: move hand to handle
            Vector3 targetPos = doorHandle.position;
            ikTarget.position = Vector3.Lerp(ikTarget.position, targetPos, Time.deltaTime * ikMoveSpeed);
            ikTarget.rotation = Quaternion.Slerp(ikTarget.rotation, doorHandle.rotation, Time.deltaTime * ikMoveSpeed);

            // Rotate camera toward door handle while grabbing
            playerCamera.rotation = Quaternion.Slerp(playerCamera.rotation, lookDownRotation, cameraRotateSpeed * Time.deltaTime);

            // Step 2: wait for delay before opening
            if (!handOnHandle && Vector3.Distance(ikTarget.position, targetPos) < 0.05f)
            {
                handOnHandle = true;
                grabStartTime = Time.time; // reset timer for door open
            }

            if (handOnHandle && !isDoorOpening && Time.time - grabStartTime >= delayBeforeOpen)
            {
                isDoorOpening = true;
            }

            // Step 3: open the door slowly
            if (isDoorOpening)
            {
                float currentY = door.localEulerAngles.y;
                if (currentY > 180f) currentY -= 360f;

                if (Mathf.Abs(currentY - targetOpenAngle) > 1f)
                {
                    float newY = Mathf.MoveTowards(currentY, targetOpenAngle, openSpeed * Time.deltaTime);
                    door.localEulerAngles = new Vector3(door.localEulerAngles.x, newY, door.localEulerAngles.z);

                    // Rotate camera upward toward seat while door is opening
                    playerCamera.rotation = Quaternion.Slerp(playerCamera.rotation, lookUpRotation, cameraRotateSpeed * Time.deltaTime);
                }
                else
                {
                    // Door fully opened
                    isGrabbing = false;
                    handOnHandle = false;
                    isDoorOpening = false;
                    doorOpened = true;
                }
            }
        }
        else if (!doorOpened)
        {
            // If not grabbing and door not opened, smoothly return camera to initial rotation
            playerCamera.rotation = Quaternion.Slerp(playerCamera.rotation, initialCamRotation, cameraRotateSpeed * Time.deltaTime);
        }
    }
}
