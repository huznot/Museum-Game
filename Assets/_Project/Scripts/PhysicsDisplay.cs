using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityTutorial.PlayerControl;

public class PhysicsDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private Rigidbody carRb;
    [SerializeField] private SitInCarController sitController;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text velocityText;
    [SerializeField] private TMP_Text accelerationText;
    [SerializeField] private TMP_Text forceText;
    [SerializeField] private RectTransform velocityArrow;
    [SerializeField] private RectTransform accelerationArrow;
    [SerializeField] private RectTransform forceArrow;

    [Header("Display Settings")]
    [SerializeField] private float accelerationWindowSeconds = 3f;
    [SerializeField] private float directionDeadZone = 0.05f;
    [SerializeField] private float velocityNoiseThreshold = 0.02f;
    [SerializeField] private float accelerationNoiseThreshold = 0.1f;
    [SerializeField] private float stopSpeedResetThreshold = 0.1f;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private float groundContactPadding = 0.02f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundedMinTime = 0.05f;
    [SerializeField] private float airborneMinTime = 0.02f;
    [SerializeField] private float landingGraceSeconds = 0.25f;
    [SerializeField] private float directionSmoothingSeconds = 0.15f;
    [SerializeField] private float velocitySmoothingSeconds = 0.1f;
    [SerializeField] private float lateralDirectionSuppressionRatio = 0.2f;
    [SerializeField] private float jumpUpwardBoostSeconds = 0.06f;
    [SerializeField] private float jumpUpwardVelocityThreshold = 0.2f;
    [SerializeField] private bool useUnscaledTime = false;

    private readonly Queue<VelocitySample> _samples = new Queue<VelocitySample>();
    private bool _hasLastPosition;
    private Vector3 _lastPosition;
    private bool _wasSitting;
    private bool _isGrounded = true;
    private bool _wasGrounded = true;
    private float _groundedTimer;
    private float _airborneTimer;
    private bool _useGravityOverride;
    private float _airborneVerticalVelocity;
    private bool _inJumpSequence;
    private float _landingGraceTimer;
    private float _jumpBoostTimer;
    private float _lastDt = 0.02f;
    private Vector2 _smoothedVelocityDir;
    private Vector2 _smoothedAccelerationDir;
    private Vector2 _smoothedForceDir;
    private Transform _trackedTransformForDisplay;
    private float _lastVelocityY;
    private Vector3 _smoothedVelocity;
    private bool _hasSmoothedVelocity;

    private struct VelocitySample
    {
        public float time;
        public Vector3 velocity;
    }

    private void Awake()
    {
        if (playerRb == null)
            TryGetComponent(out playerRb);

        if (playerController == null)
            TryGetComponent(out playerController);

        if (sitController == null)
            TryGetComponent(out sitController);

        RefreshCarReference();
    }

    private void OnEnable()
    {
        ResetTracking();
        UpdateDisplays(Vector3.zero, Vector3.zero, false);
    }

    private void Update()
    {
        RefreshCarReference();

        bool isInCar = IsSittingInCar();
        if (isInCar != _wasSitting)
        {
            ResetTracking();
        }
        _wasSitting = isInCar;

        Transform trackedTransform = GetTrackedTransform();
        if (trackedTransform == null)
        {
            _trackedTransformForDisplay = null;
            UpdateDisplays(Vector3.zero, Vector3.zero, isInCar);
            return;
        }
        _trackedTransformForDisplay = trackedTransform;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= Mathf.Epsilon)
            return;
        _lastDt = dt;

        Vector3 currentPosition = trackedTransform.position;
        if (!_hasLastPosition)
        {
            _lastPosition = currentPosition;
            _hasLastPosition = true;
            UpdateDisplays(Vector3.zero, Vector3.zero, isInCar);
            return;
        }

        Vector3 velocity = (currentPosition - _lastPosition) / dt;
        _lastPosition = currentPosition;

        if (velocity.magnitude < velocityNoiseThreshold)
            velocity = Vector3.zero;

        float velocitySmoothing = velocitySmoothingSeconds <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01(dt / velocitySmoothingSeconds);
        if (!_hasSmoothedVelocity)
        {
            _smoothedVelocity = velocity;
            _hasSmoothedVelocity = true;
        }
        else
        {
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, velocity, velocitySmoothing);
        }

        Vector3 stableVelocity = _smoothedVelocity;
        if (stableVelocity.magnitude < velocityNoiseThreshold)
            stableVelocity = Vector3.zero;

        if (isInCar && velocity.magnitude < stopSpeedResetThreshold)
        {
            ResetTracking();
            _hasLastPosition = true;
            _lastPosition = currentPosition;
            UpdateDisplays(Vector3.zero, Vector3.zero, isInCar);
            return;
        }

        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        _samples.Enqueue(new VelocitySample { time = now, velocity = stableVelocity });
        while (_samples.Count > 0 && now - _samples.Peek().time > accelerationWindowSeconds)
        {
            _samples.Dequeue();
        }

        Vector3 acceleration = Vector3.zero;
        if (_samples.Count >= 2)
        {
            VelocitySample oldest = _samples.Peek();
            float window = Mathf.Max(0.0001f, now - oldest.time);
            acceleration = (stableVelocity - oldest.velocity) / window;
        }

        _useGravityOverride = ShouldUseGravityOverride(isInCar);
        if (_useGravityOverride)
        {
            Vector3 groundRayOrigin = GetGroundRayOrigin(trackedTransform);
            float castDistance = GetGroundCheckDistance(isInCar);
            float contactDistance = GetGroundContactDistance(isInCar);
            LayerMask mask = groundMask;
            bool hasPlayerSettings = TryGetPlayerGroundSettings(out float playerDistance, out LayerMask playerMask);
            if (hasPlayerSettings)
            {
                castDistance = playerDistance + 0.1f;
                contactDistance = castDistance;
                mask = playerMask;
            }
            UpdateGroundedState(groundRayOrigin, castDistance, contactDistance, mask, dt);
        }
        else
        {
            _isGrounded = true;
            _groundedTimer = 0f;
            _airborneTimer = 0f;
            _inJumpSequence = false;
            _landingGraceTimer = 0f;
        }

        if (_useGravityOverride)
        {
            if (!_isGrounded)
            {
                _inJumpSequence = true;
                _landingGraceTimer = landingGraceSeconds;
            }
            else if (_inJumpSequence)
            {
                _landingGraceTimer -= dt;
                if (_landingGraceTimer <= 0f)
                {
                    _inJumpSequence = false;
                    _landingGraceTimer = 0f;
                }
            }

            if (_isGrounded && velocity.y - _lastVelocityY > jumpUpwardVelocityThreshold)
                _jumpBoostTimer = jumpUpwardBoostSeconds;

            if (_jumpBoostTimer > 0f)
            {
                _jumpBoostTimer -= dt;
            }
            else if (_inJumpSequence)
            {
                // Keep gravity for the full jump/fall/landing animation.
                acceleration = new Vector3(acceleration.x, Physics.gravity.y, acceleration.z);
            }

            if (_inJumpSequence && velocity.y <= 0f && _jumpBoostTimer <= 0f)
            {
                // On the way down, lock to gravity to avoid jitter.
                acceleration = new Vector3(acceleration.x, Physics.gravity.y, acceleration.z);
            }
        }
        _lastVelocityY = velocity.y;

        Vector3 displayVelocity = stableVelocity;
        if (_useGravityOverride)
        {
            if (_isGrounded)
            {
                _airborneVerticalVelocity = 0f;
            }
            else
            {
                if (_wasGrounded)
                    _airborneVerticalVelocity = velocity.y;

                _airborneVerticalVelocity += Physics.gravity.y * dt;
                displayVelocity.y = _airborneVerticalVelocity;
            }
        }
        _wasGrounded = _isGrounded;

        if (acceleration.magnitude < accelerationNoiseThreshold)
            acceleration = Vector3.zero;

        if (_useGravityOverride && _isGrounded && velocity.magnitude < stopSpeedResetThreshold)
            acceleration = Vector3.zero;

        // After landing or coming to rest, wipe history to avoid post-impact oscillation
        if (velocity.magnitude < stopSpeedResetThreshold && acceleration == Vector3.zero)
        {
            ResetTracking();
            _hasLastPosition = true;
            _lastPosition = currentPosition;
            UpdateDisplays(Vector3.zero, Vector3.zero, isInCar);
            return;
        }

        UpdateDisplays(displayVelocity, acceleration, isInCar);
    }

    private void RefreshCarReference()
    {
        if (carRb == null && sitController != null && sitController.carGameObject != null)
        {
            sitController.carGameObject.TryGetComponent(out carRb);
        }
    }

    private bool IsSittingInCar()
    {
        return sitController != null && sitController.isSitting;
    }

    private Transform GetTrackedTransform()
    {
        if (IsSittingInCar())
        {
            if (carRb != null)
                return carRb.transform;

            if (sitController != null && sitController.carGameObject != null)
                return sitController.carGameObject.transform;
        }

        if (playerRb != null)
            return playerRb.transform;

        return transform;
    }

    private float GetActiveMass(bool isInCar)
    {
        float mass = playerRb != null ? playerRb.mass : 0f;

        if (isInCar && carRb != null)
            mass += carRb.mass;

        return mass;
    }

    private void UpdateDisplays(Vector3 velocity, Vector3 acceleration, bool isInCar)
    {
        float speed = velocity.magnitude;
        float accelerationMag = acceleration.magnitude;
        float totalMass = GetActiveMass(isInCar);
        Vector3 forceVector = totalMass * acceleration;
        float force = forceVector.magnitude;

        Vector3 accelerationDisplay = acceleration;
        Vector3 forceDisplay = forceVector;
        bool allowVertical = _useGravityOverride && (_inJumpSequence || _jumpBoostTimer > 0f);
        if (!allowVertical)
        {
            accelerationDisplay.y = 0f;
            forceDisplay.y = 0f;
        }

        Vector2 velocityHorizontal = new Vector2(velocity.x, velocity.z);
        if (!isInCar)
            velocityHorizontal = StabilizeHorizontal(velocityHorizontal, _trackedTransformForDisplay);
        Vector2 accelerationHorizontal = new Vector2(accelerationDisplay.x, accelerationDisplay.z);
        Vector2 forceHorizontal = new Vector2(forceDisplay.x, forceDisplay.z);

        float smoothing = directionSmoothingSeconds <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01(_lastDt / directionSmoothingSeconds);

        _smoothedVelocityDir = SmoothDirection(_smoothedVelocityDir, velocityHorizontal, smoothing);
        _smoothedAccelerationDir = SmoothDirection(_smoothedAccelerationDir, accelerationHorizontal, smoothing);
        _smoothedForceDir = SmoothDirection(_smoothedForceDir, forceHorizontal, smoothing);

        Vector3 velocityDisplay = new Vector3(_smoothedVelocityDir.x, velocity.y, _smoothedVelocityDir.y);
        Vector3 accelerationDisplaySmoothed = new Vector3(_smoothedAccelerationDir.x, accelerationDisplay.y, _smoothedAccelerationDir.y);
        Vector3 forceDisplaySmoothed = new Vector3(_smoothedForceDir.x, forceDisplay.y, _smoothedForceDir.y);

        if (allowVertical && velocityHorizontal.magnitude > directionDeadZone)
        {
            Vector2 velDir = velocityHorizontal.normalized;
            if (accelerationHorizontal.magnitude < directionDeadZone)
                accelerationDisplaySmoothed = new Vector3(velDir.x, accelerationDisplaySmoothed.y, velDir.y);
            if (forceHorizontal.magnitude < directionDeadZone)
                forceDisplaySmoothed = new Vector3(velDir.x, forceDisplaySmoothed.y, velDir.y);
        }

        string velocityDir = FormatDirection(velocityDisplay, allowVertical);
        string accelerationDir = FormatDirection(accelerationDisplaySmoothed, allowVertical);
        string forceDir = FormatDirection(forceDisplaySmoothed, allowVertical);
        string velocityDirColored = ColorizeDirection(velocityDir, "#5ce1e6");
        string accelerationDirColored = ColorizeDirection(accelerationDir, "#00bf63");
        string forceDirColored = ColorizeDirection(forceDir, "#ff3131");

        if (velocityText != null)
            velocityText.text = $"Velocity: {speed:F2} m/s {velocityDirColored}";

        if (accelerationText != null)
            accelerationText.text = $"Acceleration: {accelerationMag:F2} m/s² {accelerationDirColored}";

        if (forceText != null)
            forceText.text = $"Force: {force:F2} N {forceDirColored}";

        SetArrowDirection(velocityArrow, velocityDisplay);
        SetArrowDirection(accelerationArrow, accelerationDisplaySmoothed);
        SetArrowDirection(forceArrow, forceDisplaySmoothed);
    }

    private string FormatDirection(Vector3 vector, bool allowVertical)
    {
        if (!allowVertical)
            vector.y = 0f;

        if (vector.magnitude < directionDeadZone)
            return "None";

        string vertical = string.Empty;
        if (allowVertical && Mathf.Abs(vector.y) > directionDeadZone)
            vertical = vector.y > 0 ? "Up" : "Down";

        string horizontal = string.Empty;
        Vector2 horizontalVector = new Vector2(vector.x, vector.z);
        if (horizontalVector.magnitude > directionDeadZone)
        {
            float angle = Mathf.Atan2(horizontalVector.x, horizontalVector.y) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f; // 0 = North, 90 = East

            string[] directions = { "North", "North-East", "East", "South-East", "South", "South-West", "West", "North-West" };
            int index = Mathf.RoundToInt(angle / 45f) % directions.Length;
            horizontal = directions[index];
        }

        if (string.IsNullOrEmpty(horizontal))
            return vertical;
        if (string.IsNullOrEmpty(vertical))
            return horizontal;

        return $"{horizontal} {vertical}";
    }

    private string ColorizeDirection(string direction, string colorHex)
    {
        if (string.IsNullOrEmpty(direction))
            return direction;

        return $"<color={colorHex}>{direction}</color>";
    }

    private void SetArrowDirection(RectTransform arrow, Vector3 vector)
    {
        if (arrow == null)
            return;

        Vector2 horizontalVector = new Vector2(vector.x, vector.z);
        if (horizontalVector.magnitude < directionDeadZone)
        {
            arrow.gameObject.SetActive(false);
            return;
        }

        arrow.gameObject.SetActive(true);
        float angle = Mathf.Atan2(horizontalVector.x, horizontalVector.y) * Mathf.Rad2Deg;
        angle = (angle + 360f) % 360f; // 0 = North (up), 90 = East (right)
        arrow.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    private void ResetTracking()
    {
        _samples.Clear();
        _hasLastPosition = false;
        _lastPosition = Vector3.zero;
        _smoothedVelocityDir = Vector2.zero;
        _smoothedAccelerationDir = Vector2.zero;
        _smoothedForceDir = Vector2.zero;
        _smoothedVelocity = Vector3.zero;
        _hasSmoothedVelocity = false;
        _jumpBoostTimer = 0f;
        _lastVelocityY = 0f;
    }

    private bool IsGrounded(Vector3 origin, float distance, LayerMask mask, out RaycastHit hit)
    {
        return Physics.Raycast(
            origin,
            Vector3.down,
            out hit,
            distance,
            mask,
            QueryTriggerInteraction.Ignore);
    }

    private void UpdateGroundedState(Vector3 origin, float castDistance, float contactDistance, LayerMask mask, float dt)
    {
        bool hitGround = IsGrounded(origin, castDistance, mask, out RaycastHit hit);
        bool rawGrounded = hitGround && hit.distance <= contactDistance;
        if (rawGrounded)
        {
            _groundedTimer = 0f;
            _airborneTimer = 0f;
            _isGrounded = true;
        }
        else
        {
            _airborneTimer += dt;
            _groundedTimer = 0f;
            if (_airborneTimer >= airborneMinTime)
                _isGrounded = false;
        }
    }

    private bool ShouldUseGravityOverride(bool isInCar)
    {
        if (isInCar)
            return false;

        if (playerRb == null)
            return false;

        if (playerRb.isKinematic || !playerRb.detectCollisions)
            return false;

        if (playerController != null && playerController.movementDisabled)
            return false;

        return true;
    }

    private Vector3 GetGroundRayOrigin(Transform trackedTransform)
    {
        if (playerRb != null && trackedTransform == playerRb.transform)
            return playerRb.worldCenterOfMass;

        return trackedTransform != null ? trackedTransform.position : transform.position;
    }

    private float GetGroundCheckDistance(bool isInCar)
    {
        if (isInCar || playerRb == null)
            return groundCheckDistance;

        if (playerController != null)
            return Mathf.Max(groundCheckDistance, playerController.GroundCheckDistance);

        Collider playerCollider = playerRb.GetComponent<Collider>();
        if (playerCollider == null)
            return groundCheckDistance;

        float colliderDistance = playerCollider.bounds.extents.y + 0.1f;
        return Mathf.Max(groundCheckDistance, colliderDistance);
    }

    private float GetGroundContactDistance(bool isInCar)
    {
        if (isInCar || playerRb == null)
            return groundCheckDistance;

        if (playerController != null)
            return Mathf.Max(groundCheckDistance, playerController.GroundCheckDistance);

        Collider playerCollider = playerRb.GetComponent<Collider>();
        if (playerCollider == null)
            return groundCheckDistance;

        return playerCollider.bounds.extents.y + groundContactPadding;
    }

    private bool TryGetPlayerGroundSettings(out float distance, out LayerMask mask)
    {
        if (playerController == null && playerRb != null)
            playerController = playerRb.GetComponent<PlayerController>();

        if (playerController == null)
        {
            distance = groundCheckDistance;
            mask = groundMask;
            return false;
        }

        distance = playerController.GroundCheckDistance;
        mask = playerController.GroundCheckMask;
        return true;
    }

    private Vector2 SmoothDirection(Vector2 current, Vector2 target, float smoothing)
    {
        if (target.magnitude < directionDeadZone)
            return Vector2.zero;

        return Vector2.Lerp(current, target, smoothing);
    }

    private Vector2 StabilizeHorizontal(Vector2 horizontal, Transform reference)
    {
        if (reference == null || horizontal.magnitude < directionDeadZone)
            return horizontal;

        Vector3 local = reference.InverseTransformDirection(new Vector3(horizontal.x, 0f, horizontal.y));
        float absX = Mathf.Abs(local.x);
        float absZ = Mathf.Abs(local.z);

        float ratio = Mathf.Clamp01(lateralDirectionSuppressionRatio);
        if (ratio > 0f)
        {
            if (absZ >= absX && absZ > Mathf.Epsilon)
            {
                float threshold = absZ * ratio;
                if (absX < threshold)
                {
                    float t = Mathf.InverseLerp(0f, threshold, absX);
                    local.x *= t;
                }
            }
            else if (absX > absZ && absX > Mathf.Epsilon)
            {
                float threshold = absX * ratio;
                if (absZ < threshold)
                {
                    float t = Mathf.InverseLerp(0f, threshold, absZ);
                    local.z *= t;
                }
            }
        }

        Vector3 world = reference.TransformDirection(local);
        return new Vector2(world.x, world.z);
    }
}
