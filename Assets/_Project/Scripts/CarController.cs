using System.Collections;
using TMPro;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Car Settings")]
    public float motorForce = 1500f;
    public float brakeForce = 3000f;
    public float maxSteerAngle = 30f;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWC;
    public WheelCollider frontRightWC;
    public WheelCollider rearLeftWC;
    public WheelCollider rearRightWC;

    [Header("Visual Wheels (Optional)")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Header("Center of Mass")]
    public Transform centerOfMass;

    [Header("Camera Shake")]
    public float shakeDuration = 1f;
    public float shakeIntensity = 0.1f;
    public AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public Transform mainCamera;

    [Header("Hill Assist")]
    public bool hillAssistEnabled = true;
    public float hillAssistStartAngle = 8f;
    public float hillAssistMaxAngle = 30f;
    public float hillAssistDownforce = 3000f;
    public float hillAssistFrictionMultiplier = 1.4f;
    public float hillAssistMinThrottle = 0.1f;
    public float hillAssistGroundCheckDistance = 2.5f;
    public LayerMask hillAssistGroundMask = ~0;
    
    // Reference to SitInCarController
    public SitInCarController sitInCarController;

    [Header("Intro Drive")]
    public bool playIntroDriveOnStart = true;
    public float introDriveDuration = 5f;
    public float introDriveThrottle = 1f;
    public float introDriveTorque = 12000f;
    public float introStartSpeed = 35f; // m/s (~126 km/h)
    public float hintDelay = 3f;
    public CanvasGroup driveHintCanvasGroup;
    public TMP_Text driveHintText;
    [TextArea] public string driveHintMessage = "Use WASD to drive";
    public float hintFadeDuration = 0.5f;
    public float hintVisibleDuration = 2f;
    
    [Header("Speedometer Needles")]
    public Transform motorNeedle;
    public Transform secondSpeedometerNeedle;
    public Transform thirdSpeedometerNeedle;
    public float motorNeedleMinAngle = -1f;
    public float motorNeedleMaxAngle = -175.36f;
    public float motorNeedleSmoothing = 6f;
    public Transform fuelNeedle;
    public float fuelNeedleStartAngle = -104.238f;
    public float fuelNeedleEmptyAngle = -56.786f;
    public float fuelNeedleDropAngle = -176.571f;
    public float fuelNeedleDurationSeconds = 60f;
    public float fuelNeedleRefuelDurationSeconds = 600f;
    public static bool triggerFuelDrop = false;
    public float fuelNeedleSmoothing = 3f;
    public Transform speedNeedle;
    public float speedNeedleMinAngle = 5.271f;
    public float speedNeedleMaxAngle = -243.085f;
    public float speedNeedleMaxSpeed = 80f; // meters per second (~288 km/h)
    public float speedNeedleSmoothing = 8f;

    [Header("Audio")]
    public AudioClip engineIdleClip;
    public AudioClip engineAccelClip;
    public AudioClip fuelEmptyAlarmClip;
    public AudioSource engineIdleSource;
    public AudioSource engineAccelSource;
    public AudioSource fuelAlarmSource;
    [Range(0f, 1f)] public float idleVolume = 0.6f;
    [Range(0f, 1f)] public float accelVolume = 0.8f;
    [Range(0f, 1f)] public float alarmVolume = 0.9f;
    public float volumeFadeSpeed = 4f;
    public float idlePitchMin = 0.9f;
    public float idlePitchMax = 1.05f;
    public float accelPitchMin = 0.95f;
    public float accelPitchMax = 1.2f;
    public float speedForMaxPitch = 30f;

    private float horizontalInput;
    private float verticalInput;
    private float steerAngle;
    private bool isBraking;
    private Rigidbody carRigidbody;
    private bool introDriveActive;
    private bool introSequenceCompleted;
    private Quaternion introHeading;
    private Coroutine hintCoroutine;
    private float lastMotorTorqueApplied;
    private float currentMotorNeedleAngle;
    private Vector3 motorNeedleBaseEuler;
    private float fuelElapsedTime;
    private float currentFuelNeedleAngle;
    private Vector3 fuelNeedleBaseEuler;
    private bool isOutOfFuel;
    private bool fuelAlarmDismissed;
    private bool fuelDropActive;
    private float fuelCycleDuration;
    private float fuelCycleStartAngle;
    private float currentSpeedNeedleAngle;
    private Vector3 speedNeedleBaseEuler;
    private bool alarmActive;
    private float rearLeftBaseForwardStiffness;
    private float rearRightBaseForwardStiffness;
    private bool hillAssistFrictionActive;
    private float accelAudioUnlockTime;
    
    // Camera shake variables - using static offset approach
    private bool isShaking = false;
    private Coroutine shakeCoroutine;
    public static Vector3 cameraShakeOffset = Vector3.zero; // Static so PlayerController can access it

    void OnEnable()
    {
        carRigidbody = GetComponent<Rigidbody>();

        // Set center of mass lower for stability
        if (centerOfMass != null)
        {
            carRigidbody.centerOfMass = centerOfMass.localPosition;
        }
        else
        {
            carRigidbody.centerOfMass = new Vector3(0, -0.5f, 0);
        }
        
        // DEBUG: Check if camera is assigned
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not assigned to CarController!");
        }
        else
        {
            Debug.Log("Main Camera assigned: " + mainCamera.name);
        }

        if (motorNeedle != null)
        {
            motorNeedleBaseEuler = motorNeedle.localEulerAngles;
            currentMotorNeedleAngle = motorNeedleMinAngle;
            ApplyMotorNeedleRotation(currentMotorNeedleAngle);
        }

        if (fuelNeedle != null)
        {
            fuelNeedleBaseEuler = fuelNeedle.localEulerAngles;
            currentFuelNeedleAngle = fuelNeedleStartAngle;
            ApplyFuelNeedleRotation(currentFuelNeedleAngle);
            fuelCycleStartAngle = fuelNeedleStartAngle;
            fuelCycleDuration = fuelNeedleDurationSeconds;
        }

        if (speedNeedle != null)
        {
            speedNeedleBaseEuler = speedNeedle.localEulerAngles;
            currentSpeedNeedleAngle = speedNeedleMinAngle;
            ApplySpeedNeedleRotation(currentSpeedNeedleAngle);
        }

        EnsureAudioSources();
        accelAudioUnlockTime = Time.time + 0.2f;

        CacheBaseFriction();
    }

    void Start()
    {
        EnsureHintCanvasGroup();

        if (driveHintCanvasGroup != null)
        {
            driveHintCanvasGroup.alpha = 0f;
        }

        if (playIntroDriveOnStart && !introSequenceCompleted)
        {
            StartCoroutine(RunIntroSequence());
        }
    }





    public void SetSitInCarController(SitInCarController controller)
    {
        sitInCarController = controller;
    }

    void Update()
    {
        // Only get input if player is sitting in car
        if (sitInCarController != null && sitInCarController.isSitting && introDriveActive)
        {
            // Intro drive ignores player input and moves forward
            horizontalInput = 0f;
            verticalInput = introDriveThrottle;
            isBraking = false;
            
            // Keep the car pinned at highway speed during intro
            if (carRigidbody != null)
            {
                // Maintain heading and suppress drift/rotation during the intro
                carRigidbody.angularVelocity = Vector3.zero;
                transform.rotation = introHeading;
                carRigidbody.linearVelocity = transform.forward * introStartSpeed;
            }
        }
        else if (sitInCarController != null && sitInCarController.isSitting)
        {
            GetInput();
            
            // Example: Trigger shake when braking hard
            if (isBraking && GetSpeed() > 10f)
            {
                Debug.Log("Auto-triggering brake shake");
            }
        }
        else
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            isBraking = false;
            
            
        }

        if (isOutOfFuel && alarmActive && Input.GetKeyDown(KeyCode.E))
        {
            fuelAlarmDismissed = true;
            SetFuelAlarm(false);
        }

    }

    public float GetSpeed()
    {
        if (carRigidbody != null)
            return carRigidbody.linearVelocity.magnitude; // speed in meters/sec
        return 0f;
    }

    public bool IsOutOfFuel()
    {
        return isOutOfFuel;
    }

    void LateUpdate()
    {
        // Only handle driving if player is sitting in car
        if (sitInCarController != null && sitInCarController.isSitting)
        {
            HandleMotor();
            HandleSteering();
        }
        else
        {
            // Ensure car doesn't move if not sitting
            HandleBrakesWhenNotSitting();
            lastMotorTorqueApplied = 0f;
        }

        UpdateWheelVisuals();
        UpdateNeedleGauges();
        UpdateEngineAudio();
    }

    void FixedUpdate()
    {
        if (sitInCarController != null && sitInCarController.isSitting)
        {
            ApplyHillAssist();
        }
        else
        {
            ResetHillAssistFriction();
        }
    }


    void GetInput()
    {
        horizontalInput = Input.GetAxis("Horizontal"); // A/D keys
        verticalInput = Input.GetAxis("Vertical");     // W/S keys
        isBraking = Input.GetKey(KeyCode.Space);       // Spacebar for brake
    }

    void HandleMotor()
    {
        // Apply motor force to rear wheels
        bool usingIntroDrive = introDriveActive && sitInCarController != null && sitInCarController.isSitting;
        float motorTorque = usingIntroDrive ? introDriveTorque : verticalInput * motorForce;
        lastMotorTorqueApplied = motorTorque;

        rearLeftWC.motorTorque = motorTorque;
        rearRightWC.motorTorque = motorTorque;

        // Apply braking force to all wheels
        float currentBrakeForce = isBraking ? brakeForce : 0f;
        frontLeftWC.brakeTorque = currentBrakeForce;
        frontRightWC.brakeTorque = currentBrakeForce;
        rearLeftWC.brakeTorque = currentBrakeForce;
        rearRightWC.brakeTorque = currentBrakeForce;
    }

    void HandleSteering()
    {
        // Calculate steering angle
        steerAngle = maxSteerAngle * horizontalInput;
        
        // Apply steering to front wheels
        frontLeftWC.steerAngle = steerAngle;
        frontRightWC.steerAngle = steerAngle;
    }

    void HandleBrakesWhenNotSitting()
    {
        // Set all torques and steering to zero, and apply full brake
        rearLeftWC.motorTorque = 0f;
        rearRightWC.motorTorque = 0f;
        frontLeftWC.motorTorque = 0f;
        frontRightWC.motorTorque = 0f;

        frontLeftWC.steerAngle = 0f;
        frontRightWC.steerAngle = 0f;

        float fullBrake = brakeForce;
        frontLeftWC.brakeTorque = fullBrake;
        frontRightWC.brakeTorque = fullBrake;
        rearLeftWC.brakeTorque = fullBrake;
        rearRightWC.brakeTorque = fullBrake;
    }

    void UpdateNeedleGauges()
    {
        UpdateMotorNeedle();
        UpdateFuelNeedle();
        UpdateSpeedNeedle();
    }

    void UpdateMotorNeedle()
    {
        if (motorNeedle == null) return;

        float maxTorque = introDriveActive ? introDriveTorque : motorForce;
        maxTorque = Mathf.Max(1f, maxTorque); // avoid division by zero
        float normalizedTorque = Mathf.Clamp01(Mathf.Abs(lastMotorTorqueApplied) / maxTorque);
        float targetAngle = Mathf.Lerp(motorNeedleMinAngle, motorNeedleMaxAngle, normalizedTorque);

        currentMotorNeedleAngle = Mathf.Lerp(currentMotorNeedleAngle, targetAngle, Time.deltaTime * motorNeedleSmoothing);
        ApplyMotorNeedleRotation(currentMotorNeedleAngle);
    }

    void ApplyMotorNeedleRotation(float zAngle)
    {
        motorNeedle.localRotation = Quaternion.Euler(motorNeedleBaseEuler.x, motorNeedleBaseEuler.y, zAngle);
    }

    void UpdateFuelNeedle()
    {
        if (fuelNeedle == null) return;

        if (triggerFuelDrop)
        {
            fuelDropActive = true;
            triggerFuelDrop = false;
            isOutOfFuel = false;
            fuelAlarmDismissed = false;
            motorForce = 1000f;
            fuelElapsedTime = 0f;
            fuelCycleStartAngle = fuelNeedleDropAngle;
            fuelCycleDuration = fuelNeedleRefuelDurationSeconds;
            SetFuelAlarm(false);
        }

        if (!isOutOfFuel)
        {
            fuelElapsedTime += Time.deltaTime;
        }

        float duration = Mathf.Max(0.0001f, fuelCycleDuration <= 0f ? fuelNeedleDurationSeconds : fuelCycleDuration);
        float fuelProgress = Mathf.Clamp01(fuelElapsedTime / duration);
        float baselineTarget = Mathf.Lerp(fuelCycleStartAngle, fuelNeedleEmptyAngle, fuelProgress);
        float targetAngle = fuelDropActive ? fuelNeedleDropAngle : baselineTarget;

        currentFuelNeedleAngle = Mathf.Lerp(currentFuelNeedleAngle, targetAngle, Time.deltaTime * fuelNeedleSmoothing);
        ApplyFuelNeedleRotation(currentFuelNeedleAngle);

        if (fuelDropActive && Mathf.Abs(currentFuelNeedleAngle - fuelNeedleDropAngle) < 0.1f)
        {
            fuelDropActive = false;
            // Start next drain cycle from the refuel drop angle
            fuelCycleStartAngle = fuelNeedleDropAngle;
            fuelElapsedTime = 0f;
        }

        if (!isOutOfFuel && fuelProgress >= 1f)
        {
            motorForce = 0f;
            isOutOfFuel = true;
            if (!fuelAlarmDismissed)
                SetFuelAlarm(true);
        }
    }

    void ApplyHillAssist()
    {
        if (!hillAssistEnabled || carRigidbody == null)
        {
            ResetHillAssistFriction();
            return;
        }

        float throttle = Mathf.Clamp01(Mathf.Abs(verticalInput));
        if (throttle < hillAssistMinThrottle)
        {
            ResetHillAssistFriction();
            return;
        }

        RaycastHit hit;
        Vector3 origin = centerOfMass != null ? centerOfMass.position : transform.position;
        if (!Physics.Raycast(origin, -transform.up, out hit, hillAssistGroundCheckDistance, hillAssistGroundMask))
        {
            ResetHillAssistFriction();
            return;
        }

        float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
        if (slopeAngle < hillAssistStartAngle)
        {
            ResetHillAssistFriction();
            return;
        }

        Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
        Vector3 uphill = -downhill;
        float facingUphill = Vector3.Dot(transform.forward, uphill);
        if (facingUphill <= 0.2f)
        {
            ResetHillAssistFriction();
            return;
        }

        float slopeT = Mathf.InverseLerp(hillAssistStartAngle, Mathf.Max(hillAssistStartAngle + 0.01f, hillAssistMaxAngle), slopeAngle);
        float extraDownforce = hillAssistDownforce * slopeT * throttle;
        carRigidbody.AddForce(-transform.up * extraDownforce, ForceMode.Force);

        float targetMultiplier = Mathf.Lerp(1f, hillAssistFrictionMultiplier, slopeT);
        ApplyHillAssistFriction(targetMultiplier);
    }

    void CacheBaseFriction()
    {
        if (rearLeftWC != null)
            rearLeftBaseForwardStiffness = rearLeftWC.forwardFriction.stiffness;
        if (rearRightWC != null)
            rearRightBaseForwardStiffness = rearRightWC.forwardFriction.stiffness;
    }

    void ApplyHillAssistFriction(float multiplier)
    {
        if (rearLeftWC != null)
        {
            WheelFrictionCurve friction = rearLeftWC.forwardFriction;
            friction.stiffness = rearLeftBaseForwardStiffness * multiplier;
            rearLeftWC.forwardFriction = friction;
        }

        if (rearRightWC != null)
        {
            WheelFrictionCurve friction = rearRightWC.forwardFriction;
            friction.stiffness = rearRightBaseForwardStiffness * multiplier;
            rearRightWC.forwardFriction = friction;
        }

        hillAssistFrictionActive = true;
    }

    void ResetHillAssistFriction()
    {
        if (!hillAssistFrictionActive)
            return;

        if (rearLeftWC != null)
        {
            WheelFrictionCurve friction = rearLeftWC.forwardFriction;
            friction.stiffness = rearLeftBaseForwardStiffness;
            rearLeftWC.forwardFriction = friction;
        }

        if (rearRightWC != null)
        {
            WheelFrictionCurve friction = rearRightWC.forwardFriction;
            friction.stiffness = rearRightBaseForwardStiffness;
            rearRightWC.forwardFriction = friction;
        }

        hillAssistFrictionActive = false;
    }

    void ApplyFuelNeedleRotation(float zAngle)
    {
        fuelNeedle.localRotation = Quaternion.Euler(fuelNeedleBaseEuler.x, fuelNeedleBaseEuler.y, zAngle);
    }

    void UpdateSpeedNeedle()
    {
        if (speedNeedle == null) return;

        float speed = GetSpeed(); // m/s
        float maxSpeed = Mathf.Max(0.01f, speedNeedleMaxSpeed);
        float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
        float targetAngle = Mathf.Lerp(speedNeedleMinAngle, speedNeedleMaxAngle, normalizedSpeed);

        currentSpeedNeedleAngle = Mathf.Lerp(currentSpeedNeedleAngle, targetAngle, Time.deltaTime * speedNeedleSmoothing);
        ApplySpeedNeedleRotation(currentSpeedNeedleAngle);
    }

    void ApplySpeedNeedleRotation(float zAngle)
    {
        speedNeedle.localRotation = Quaternion.Euler(speedNeedleBaseEuler.x, speedNeedleBaseEuler.y, zAngle);
    }

    void EnsureAudioSources()
    {
        if (engineIdleSource == null)
        {
            engineIdleSource = gameObject.AddComponent<AudioSource>();
            engineIdleSource.playOnAwake = false;
            engineIdleSource.loop = true;
            engineIdleSource.spatialBlend = 1f;
        }

        if (engineAccelSource == null)
        {
            engineAccelSource = gameObject.AddComponent<AudioSource>();
            engineAccelSource.playOnAwake = false;
            engineAccelSource.loop = true;
            engineAccelSource.spatialBlend = 1f;
        }

        if (fuelAlarmSource == null)
        {
            fuelAlarmSource = gameObject.AddComponent<AudioSource>();
            fuelAlarmSource.playOnAwake = false;
            fuelAlarmSource.loop = true;
            fuelAlarmSource.spatialBlend = 1f;
        }

    }

    void UpdateEngineAudio()
    {
        bool isSitting = sitInCarController != null && sitInCarController.isSitting;
        if (!isSitting)
        {
            StopEngineAudio();
            SetFuelAlarm(false);
            return;
        }

        bool usingIntroDrive = introDriveActive && sitInCarController != null && sitInCarController.isSitting;
        float throttle = usingIntroDrive ? 1f : Mathf.Clamp01(Mathf.Abs(verticalInput));
        float speed = GetSpeed();
        float pitchT = Mathf.Clamp01(speed / Mathf.Max(0.01f, speedForMaxPitch));

        if (engineIdleClip != null && engineIdleSource != null)
        {
            if (engineIdleSource.clip != engineIdleClip)
                engineIdleSource.clip = engineIdleClip;

            engineIdleSource.pitch = Mathf.Lerp(idlePitchMin, idlePitchMax, pitchT);
            float idleTarget = isOutOfFuel ? 0f : Mathf.Lerp(idleVolume, idleVolume * 0.35f, throttle);
            engineIdleSource.volume = Mathf.MoveTowards(engineIdleSource.volume, idleTarget, volumeFadeSpeed * Time.deltaTime);

            if (engineIdleSource.volume > 0.01f)
            {
                if (!engineIdleSource.isPlaying)
                    engineIdleSource.Play();
            }
            else if (engineIdleSource.isPlaying)
            {
                engineIdleSource.Stop();
            }
        }

        if (engineAccelClip != null && engineAccelSource != null)
        {
            if (engineAccelSource.clip != engineAccelClip)
                engineAccelSource.clip = engineAccelClip;

            engineAccelSource.pitch = Mathf.Lerp(accelPitchMin, accelPitchMax, pitchT);
            bool accelUnlocked = Time.time >= accelAudioUnlockTime;
            float accelTarget = (!isOutOfFuel && accelUnlocked && throttle > 0.05f) ? accelVolume : 0f;
            engineAccelSource.volume = Mathf.MoveTowards(engineAccelSource.volume, accelTarget, volumeFadeSpeed * Time.deltaTime);

            if (engineAccelSource.volume > 0.01f)
            {
                if (!engineAccelSource.isPlaying)
                    engineAccelSource.Play();
            }
            else if (engineAccelSource.isPlaying)
            {
                engineAccelSource.Stop();
            }
        }

        if (!isOutOfFuel)
            SetFuelAlarm(false);
    }

    void StopEngineAudio()
    {
        if (engineIdleSource != null && engineIdleSource.isPlaying)
            engineIdleSource.Stop();
        if (engineAccelSource != null && engineAccelSource.isPlaying)
            engineAccelSource.Stop();
    }

    void SetFuelAlarm(bool active)
    {
        if (alarmActive == active) return;
        alarmActive = active;

        if (fuelAlarmSource == null || fuelEmptyAlarmClip == null)
            return;

        fuelAlarmSource.clip = fuelEmptyAlarmClip;
        fuelAlarmSource.volume = alarmVolume;

        if (active)
        {
            if (!fuelAlarmSource.isPlaying)
                fuelAlarmSource.Play();
        }
        else
        {
            if (fuelAlarmSource.isPlaying)
                fuelAlarmSource.Stop();
        }
    }

    void UpdateWheelVisuals()
    {
        // Update visual wheel positions and rotations
        if (frontLeftWheel != null)
            UpdateSingleWheel(frontLeftWC, frontLeftWheel);
        if (frontRightWheel != null)
            UpdateSingleWheel(frontRightWC, frontRightWheel);
        if (rearLeftWheel != null)
            UpdateSingleWheel(rearLeftWC, rearLeftWheel);
        if (rearRightWheel != null)
            UpdateSingleWheel(rearRightWC, rearRightWheel);
    }

    void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    // Optional: Method to enable/disable driving (call from SitInCarController)
    public void SetDrivingEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    private IEnumerator RunIntroSequence()
    {
        introSequenceCompleted = true;
        introDriveActive = true;
        introHeading = transform.rotation;

        // Jump-start velocity so it feels already moving
        if (carRigidbody != null)
            carRigidbody.linearVelocity = transform.forward * introStartSpeed;

        float elapsed = 0f;
        bool hintStarted = false;
        while (elapsed < introDriveDuration)
        {
            elapsed += Time.deltaTime;

            if (!hintStarted && elapsed >= hintDelay)
            {
                hintStarted = true;
                if (hintCoroutine == null)
                    hintCoroutine = StartCoroutine(ShowHint());
            }

            yield return null;
        }

        introDriveActive = false;
    }

    private IEnumerator ShowHint()
    {
        if (driveHintCanvasGroup == null) yield break;

        if (!driveHintCanvasGroup.gameObject.activeSelf)
            driveHintCanvasGroup.gameObject.SetActive(true);

        if (driveHintText != null)
            driveHintText.text = driveHintMessage;

        // Keep the hint within the intro window so fade-out ends with the auto-drive
        float availableWindow = Mathf.Max(0f, introDriveDuration - hintDelay);
        float holdTime = Mathf.Max(0f, Mathf.Min(hintVisibleDuration, availableWindow) - 2f * hintFadeDuration);

        yield return StartCoroutine(FadeCanvasGroup(driveHintCanvasGroup, 0f, 1f, hintFadeDuration));
        yield return new WaitForSeconds(holdTime);
        yield return StartCoroutine(FadeCanvasGroup(driveHintCanvasGroup, 1f, 0f, hintFadeDuration));

        hintCoroutine = null;
    }

    private void EnsureHintCanvasGroup()
    {
        if (driveHintCanvasGroup != null) return;

        if (driveHintText != null)
        {
            driveHintCanvasGroup = driveHintText.GetComponentInParent<CanvasGroup>();
            if (driveHintCanvasGroup == null)
            {
                // Add a CanvasGroup to the text's GameObject so we can fade it
                driveHintCanvasGroup = driveHintText.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = from;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
