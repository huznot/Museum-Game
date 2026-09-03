using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations.Rigging;

public class ElectricBoxCoverIK : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform cover;
    public Transform coverHandle;
    public Transform ikTarget;
    public Rig rigLayer;
    public TwoBoneIKConstraint ikConstraint;

    [Header("Interaction")]
    public float activationDistance = 2.0f;
    public float ikMoveSpeed = 5.0f;
    public float delayBeforeOpen = 0.25f;
    public float delayBeforeClose = 0.25f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Cover Motion")]
    public float openAngle = 110.0f;
    public float openSpeed = 90.0f;
    public float closeSpeed = 90.0f;
    public Vector3 localHingeAxis = Vector3.up;
    public float openDirection = 1.0f;

    [Header("UI Prompts")]
    public GameObject promptStep1;
    public GameObject promptStep2;
    public GameObject promptStep3;
    public GameObject promptStep4;
    public GameObject promptStep5;
    public GameObject promptStep6;

    [Header("Wire Task")]
    [Tooltip("Seconds to hold the interact key to complete a wire task.")]
    public float wireTaskHoldSeconds = 3f;
    [Tooltip("UI Image (fill) used as progress bar for wire tasks.")]
    public Image wireTaskProgressImage;
    public AudioSource wireTaskAudioSource;
    public AudioClip wireTaskCompleteClip;
    [Range(0f, 1f)] public float wireTaskCompleteVolume = 1f;
    [Header("Wire Task 1 Objects")]
    public GameObject[] wireTask1DisableObjects;
    public GameObject[] wireTask1EnableObjects;
    [Header("Wire Task 2 Objects")]
    public GameObject[] wireTask2DisableObjects;
    public GameObject[] wireTask2EnableObjects;
    [Header("Wire Task 3 Objects")]
    public GameObject[] wireTask3DisableObjects;
    public GameObject[] wireTask3EnableObjects;

    [Header("Linked Actions")]
    public LeverPullIK leverPull;

    private bool isGrabbing = false;
    private bool handOnHandle = false;
    private bool isOpening = false;
    private bool isClosing = false;
    private bool coverOpened = false;
    private bool actionClose = false;
    private float grabStartTime = 0f;
    private int stepIndex = 0; // 0=open, 1=wire1, 2=wire2, 3=wire3, 4=lever, 5=close
    private float wireHoldTimer = 0f;
    private bool isHoldingWireTask = false;

    private Quaternion _closedLocalRot;
    private Quaternion _openLocalRot;

    void Start()
    {
        if (player == null || cover == null || coverHandle == null || ikTarget == null || rigLayer == null)
        {
            Debug.LogError("[ElectricBoxCoverIK] Missing references. Assign all required fields.");
            enabled = false;
            return;
        }

        _closedLocalRot = cover.localRotation;
        Vector3 axis = localHingeAxis.sqrMagnitude > 0.0001f ? localHingeAxis.normalized : Vector3.up;
        _openLocalRot = _closedLocalRot * Quaternion.AngleAxis(openAngle * openDirection, axis);

        SetAllPrompts(false);
        UpdateWireTaskProgress(0f, false);
        EnsureWireTaskAudioSource();
    }

    void Update()
    {
        float dist = Vector3.Distance(player.position, coverHandle.position);
        bool inRange = dist < activationDistance;

        UpdatePrompts(inRange);

        if (inRange && Input.GetKeyDown(interactKey) && !isOpening && !isClosing)
        {
            if (stepIndex == 0)
            {
                actionClose = false;
                BeginGrab();
            }
            else if (stepIndex == 4)
            {
                if (leverPull == null || leverPull.TriggerPull())
                    stepIndex = 5;
            }
            else if (stepIndex == 5)
            {
                actionClose = true;
                BeginGrab();
            }
        }

        if (!inRange)
        {
            isGrabbing = false;
            isOpening = false;
            isClosing = false;
            handOnHandle = false;
            ResetWireHold();
        }

        float targetWeight = isGrabbing ? 1f : 0f;
        if (rigLayer != null)
            rigLayer.weight = Mathf.Lerp(rigLayer.weight, targetWeight, Time.deltaTime * 6f);
        if (ikConstraint != null)
            ikConstraint.weight = Mathf.Lerp(ikConstraint.weight, targetWeight, Time.deltaTime * 6f);

        if (isGrabbing)
        {
            Vector3 targetPos = coverHandle.position;
            ikTarget.position = Vector3.Lerp(ikTarget.position, targetPos, Time.deltaTime * ikMoveSpeed);
            ikTarget.rotation = Quaternion.Slerp(ikTarget.rotation, coverHandle.rotation, Time.deltaTime * ikMoveSpeed);

            if (!handOnHandle && Vector3.Distance(ikTarget.position, targetPos) < 0.05f)
            {
                handOnHandle = true;
                grabStartTime = Time.time;
            }

            if (handOnHandle && !isOpening && !isClosing)
            {
                float delay = actionClose ? delayBeforeClose : delayBeforeOpen;
                if (Time.time - grabStartTime >= delay)
                {
                    if (actionClose) isClosing = true;
                    else isOpening = true;
                }
            }

            if (isOpening)
            {
                cover.localRotation = Quaternion.RotateTowards(cover.localRotation, _openLocalRot, openSpeed * Time.deltaTime);

                if (Quaternion.Angle(cover.localRotation, _openLocalRot) <= 0.5f)
                {
                    cover.localRotation = _openLocalRot;
                    isGrabbing = false;
                    handOnHandle = false;
                    isOpening = false;
                    coverOpened = true;
                    stepIndex = 1;
                }
            }

            if (isClosing)
            {
                cover.localRotation = Quaternion.RotateTowards(cover.localRotation, _closedLocalRot, closeSpeed * Time.deltaTime);

                if (Quaternion.Angle(cover.localRotation, _closedLocalRot) <= 0.5f)
                {
                    cover.localRotation = _closedLocalRot;
                    isGrabbing = false;
                    handOnHandle = false;
                    isClosing = false;
                    coverOpened = false;
                    stepIndex = 0;
                }
            }
        }

        HandleWireTasks(inRange);
    }

    private void BeginGrab()
    {
        isGrabbing = true;
        handOnHandle = false;
        isOpening = false;
        isClosing = false;
        grabStartTime = Time.time;
    }

    private void UpdatePrompts(bool inRange)
    {
        if (!inRange || isGrabbing || isOpening || isClosing)
        {
            SetAllPrompts(false);
            return;
        }

        SetAllPrompts(false);
        if (stepIndex == 0) SetPrompt(promptStep1, true);
        else if (stepIndex == 1) SetPrompt(promptStep2, true);
        else if (stepIndex == 2) SetPrompt(promptStep3, true);
        else if (stepIndex == 3) SetPrompt(promptStep4, true);
        else if (stepIndex == 4) SetPrompt(promptStep5, true);
        else if (stepIndex == 5) SetPrompt(promptStep6, true);
    }

    private void SetAllPrompts(bool active)
    {
        SetPrompt(promptStep1, active);
        SetPrompt(promptStep2, active);
        SetPrompt(promptStep3, active);
        SetPrompt(promptStep4, active);
        SetPrompt(promptStep5, active);
        SetPrompt(promptStep6, active);
    }

    private void SetPrompt(GameObject prompt, bool active)
    {
        if (prompt != null && prompt.activeSelf != active)
            prompt.SetActive(active);
    }

    private void HandleWireTasks(bool inRange)
    {
        if (!inRange || stepIndex < 1 || stepIndex > 3)
        {
            ResetWireHold();
            return;
        }

        // Single click interaction for wire tasks (no hold/progress bar).
        if (Input.GetKeyDown(interactKey))
        {
            CompleteWireTask(stepIndex);
            stepIndex++;
            ResetWireHold();
        }
    }

    private void CompleteWireTask(int taskIndex)
    {
        if (taskIndex == 1)
            ApplyTaskObjects(wireTask1DisableObjects, wireTask1EnableObjects);
        else if (taskIndex == 2)
            ApplyTaskObjects(wireTask2DisableObjects, wireTask2EnableObjects);
        else if (taskIndex == 3)
            ApplyTaskObjects(wireTask3DisableObjects, wireTask3EnableObjects);

        if (wireTaskAudioSource != null && wireTaskCompleteClip != null)
            wireTaskAudioSource.PlayOneShot(wireTaskCompleteClip, wireTaskCompleteVolume);
    }

    private void ApplyTaskObjects(GameObject[] disableObjects, GameObject[] enableObjects)
    {
        if (disableObjects != null)
        {
            foreach (var obj in disableObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (enableObjects != null)
        {
            foreach (var obj in enableObjects)
            {
                if (obj != null) obj.SetActive(true);
            }
        }
    }

    private void ResetWireHold()
    {
        isHoldingWireTask = false;
        wireHoldTimer = 0f;
        UpdateWireTaskProgress(0f, false);
    }

    private void UpdateWireTaskProgress(float progress, bool visible)
    {
        if (wireTaskProgressImage == null) return;
        wireTaskProgressImage.fillAmount = Mathf.Clamp01(progress);
        if (wireTaskProgressImage.gameObject.activeSelf != visible)
            wireTaskProgressImage.gameObject.SetActive(visible);
    }

    private void EnsureWireTaskAudioSource()
    {
        if (wireTaskAudioSource != null) return;
        wireTaskAudioSource = gameObject.AddComponent<AudioSource>();
        wireTaskAudioSource.playOnAwake = false;
        wireTaskAudioSource.loop = false;
        wireTaskAudioSource.spatialBlend = 0f;
    }
}
