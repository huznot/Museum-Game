using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTutorial.Manager;

namespace UnityTutorial.PlayerControl
{
    public class FlashbackPlayerController : MonoBehaviour
    {
        [SerializeField] private float AnimBlendSpeed = 8.9f;
        [SerializeField] private Transform CameraRoot;
        [SerializeField] private Transform Camera;
        [SerializeField] private float UpperLimit = -40f;
        [SerializeField] private float BottomLimit = 70f;
        [SerializeField] private float MouseSensitivity = 70f;
        [SerializeField, Range(10, 500)] private float JumpFactor = 260f;
        [SerializeField] private float Dis2Ground = 0.8f;
        [SerializeField] private LayerMask GroundCheck;
        [SerializeField] private float AirResistance = 0.8f;
        [Header("Footsteps (Tag-Based)")]
        [SerializeField] private AudioClip footstepSandClip;
        [SerializeField] private AudioClip footstepRoadClip;
        [SerializeField] private AudioClip footstepSnowClip;
        [SerializeField] private AudioSource footstepSource;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.85f;
        [SerializeField] private float footstepPitchMin = 0.95f;
        [SerializeField] private float footstepPitchMax = 1.05f;
        [SerializeField] private float minSpeedForSteps = 0.1f;
        [SerializeField] private float walkStepInterval = 0.5f;
        [SerializeField] private string sandTag = "Sand";
        [SerializeField] private string roadTag = "Road";
        [SerializeField] private string snowTag = "Snow";

        public bool jumpEnabled = false;
        public bool movementDisabled = false;
        public bool uiMode = false;

        [Header("Intro Sequence")]
        [Tooltip("If true, plays a short sitting idle, then stand-up, then enables movement.")]
        [SerializeField] private bool playIntroSequence = true;
        [SerializeField] private float sitIntroDuration = 2f;

        [Tooltip("Seconds to wait after triggering stand-up before enabling movement. Set to your stand-up clip length (~0.7-1.2).")]
        [SerializeField] private float standUpUnlockDelay = 0f;

        [Tooltip("Animator Trigger name that transitions SittingIdle -> StandUp.")]
        [SerializeField] private string introStandTriggerName = "IntroStand";
        
        [Header("Flashback SFX")]
        public AudioClip flashbackSfxClip;
        [Range(0f, 1f)] public float flashbackSfxVolume = 0.95f;
        public float flashbackSfxDelay = 2f;
        public AudioSource flashbackSfxSource;

        [Header("Ambient Wind")]
        public AudioClip windAmbientClip;
        [Range(0f, 1f)] public float windAmbientVolume = 0.25f;
        public AudioSource windAmbientSource;

        private Rigidbody _playerRigidbody;
        private InputManager _inputManager;
        private float _sittingYRotation = 0f;
        private Animator _animator;
        private bool _grounded = false;
        private bool _hasAnimator;

        private int _xVelHash;
        private int _yVelHash;
        private int _jumpHash;
        private int _groundHash;
        private int _fallingHash;
        private int _zVelHash;
        private int _introStandHash;

        private float _xRotation;
        private float _footstepTimer;

        private const float _walkSpeed = 3f;
        private Vector2 _currentVelocity;

        public float GroundCheckDistance => Dis2Ground;
        public LayerMask GroundCheckMask => GroundCheck;

        private void Start()
        {
            _hasAnimator = TryGetComponent<Animator>(out _animator);
            _playerRigidbody = GetComponent<Rigidbody>();
            _inputManager = GetComponent<InputManager>();

            var sitController = GetComponent<SitInCarController>();
            if (sitController != null && sitController.isSitting)
            {

            }

            _xVelHash = Animator.StringToHash("X_Velocity");
            _yVelHash = Animator.StringToHash("Y_Velocity");
            _zVelHash = Animator.StringToHash("Z_Velocity");
            _jumpHash = Animator.StringToHash("Jump");
            _groundHash = Animator.StringToHash("Grounded");
            _fallingHash = Animator.StringToHash("Falling");
            _introStandHash = Animator.StringToHash(introStandTriggerName);

            EnsureFootstepSource();
            EnsureFlashbackSfxSource();
            EnsureWindAmbientSource();
            if (flashbackSfxClip != null)
                StartCoroutine(PlayFlashbackSfxAfterDelay());
            PlayWindAmbient();

            // Intro: sit for a few seconds, stand up, then enable movement
            if (playIntroSequence)
            {
                movementDisabled = true;
                // keep jump disabled during flashback unless you want it
                // jumpEnabled = false;
                StartCoroutine(IntroSequence());
            }
        }

        private IEnumerator IntroSequence()
        {
            // Wait while the Animator is in your SittingIdle state (Entry -> SittingIdle should be default)
            yield return new WaitForSeconds(sitIntroDuration);
                var startSittingPosition = new Vector3(0.8700000047683716f, 2.760999917984009f, 8.607999801635743f);
                var startSittingRotation = new Quaternion(0.0f, 0.9905120730400086f, 0.0f, 0.13742591440677644f);

                if (_playerRigidbody != null)
                {
                    _playerRigidbody.position = startSittingPosition;
                    _playerRigidbody.rotation = startSittingRotation;
                    _playerRigidbody.linearVelocity = Vector3.zero;
                    _playerRigidbody.angularVelocity = Vector3.zero;
                }
                else
                {
                    transform.SetPositionAndRotation(startSittingPosition, startSittingRotation);
                }

            if (_hasAnimator)
            {
                _animator.SetTrigger(_introStandHash);
            }

            // Wait for stand-up animation to finish (set to your clip length)
            yield return new WaitForSeconds(standUpUnlockDelay);

            movementDisabled = false;
        }

        private void EnsureFlashbackSfxSource()
        {
            if (flashbackSfxSource != null) return;
            flashbackSfxSource = gameObject.AddComponent<AudioSource>();
            flashbackSfxSource.playOnAwake = false;
            flashbackSfxSource.loop = false;
            flashbackSfxSource.spatialBlend = 0f;
        }

        private void EnsureWindAmbientSource()
        {
            if (windAmbientSource != null) return;
            windAmbientSource = gameObject.AddComponent<AudioSource>();
            windAmbientSource.playOnAwake = false;
            windAmbientSource.loop = true;
            windAmbientSource.spatialBlend = 0f;
        }

        private void PlayWindAmbient()
        {
            if (windAmbientClip == null || windAmbientSource == null) return;
            windAmbientSource.clip = windAmbientClip;
            windAmbientSource.volume = windAmbientVolume;
            if (!windAmbientSource.isPlaying)
                windAmbientSource.Play();
        }

        private IEnumerator PlayFlashbackSfxAfterDelay()
        {
            yield return new WaitForSeconds(flashbackSfxDelay);
            if (flashbackSfxClip == null || flashbackSfxSource == null) yield break;
            flashbackSfxSource.volume = flashbackSfxVolume;
            flashbackSfxSource.PlayOneShot(flashbackSfxClip);
        }

        // Optional: If you add an Animation Event at the end of StandUp, call this instead of using standUpUnlockDelay.
        public void OnStandUpFinished()
        {
            movementDisabled = false;
        }

        private void FixedUpdate()
        {
            var sitController = GetComponent<SitInCarController>();
            bool isSitting = sitController != null && sitController.isSitting;

            if (isSitting)
            {
                // COMPLETELY SKIP ALL PHYSICS WHEN SITTING
                return;
            }

            SampleGround();

            if (!movementDisabled && !uiMode)
            {
                Move();
                if (jumpEnabled)
                    HandleJump();
                HandleFootsteps();
            }
        }

        private void LateUpdate()
        {
            var sitController = GetComponent<SitInCarController>();
            bool isSitting = sitController != null && sitController.isSitting;

            // ONLY do camera movements if NOT sitting and not in UI mode
            if (!isSitting && !uiMode)
            {
                if (movementDisabled)
                {
                    IntroCamPitchOnly();
                }
                else
                {
                    CamMovements();
                }
            }
            else
            {
                // When sitting, still apply camera shake if it's happening
                if (CarController.cameraShakeOffset != Vector3.zero)
                {
                    Camera.position += CarController.cameraShakeOffset;
                    Debug.Log("Applying shake while sitting: " + CarController.cameraShakeOffset);
                }
            }
        }

        private void Move()
        {
            if (!_hasAnimator) return;

            float targetSpeed = _walkSpeed;
            if (_inputManager.Move == Vector2.zero) targetSpeed = 0;

            // Always lerp regardless of grounded state — stops stale velocity
            // propelling the player when briefly ungrounded on a ramp
            _currentVelocity.x = Mathf.Lerp(_currentVelocity.x, _inputManager.Move.x * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);
            _currentVelocity.y = Mathf.Lerp(_currentVelocity.y, _inputManager.Move.y * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);

            if (_grounded)
            {
                // Compare in local space — rigidbody velocity is world space so convert it
                var localVelocity = transform.InverseTransformVector(_playerRigidbody.linearVelocity);
                var xVelDifference = _currentVelocity.x - localVelocity.x;
                var zVelDifference = _currentVelocity.y - localVelocity.z;

                float maxSafeDiff = 10f;
                xVelDifference = Mathf.Clamp(xVelDifference, -maxSafeDiff, maxSafeDiff);
                zVelDifference = Mathf.Clamp(zVelDifference, -maxSafeDiff, maxSafeDiff);

                _playerRigidbody.AddForce(transform.TransformVector(new Vector3(xVelDifference, 0, zVelDifference)), ForceMode.VelocityChange);

                // Cancel downward velocity when grounded so gravity doesn't
                // accumulate and slide the player down the ramp
                if (_playerRigidbody.linearVelocity.y < 0f)
                {
                    Vector3 vel = _playerRigidbody.linearVelocity;
                    vel.y = 0f;
                    _playerRigidbody.linearVelocity = vel;
                }
            }
            else
            {
                _playerRigidbody.AddForce(transform.TransformVector(new Vector3(_currentVelocity.x * AirResistance, 0, _currentVelocity.y * AirResistance)), ForceMode.VelocityChange);
            }

            _animator.SetFloat(_xVelHash, _currentVelocity.x);
            _animator.SetFloat(_yVelHash, _currentVelocity.y);
        }

        private void CamMovements()
        {
            if (!_hasAnimator) return;

            // Reset sitting rotation when not sitting
            _sittingYRotation = 0f;

            // NORMAL CAMERA MOVEMENT
            var Mouse_X = _inputManager.Look.x;
            var Mouse_Y = _inputManager.Look.y;

            // Set base camera position
            Camera.position = CameraRoot.position;

            // ADD CAMERA SHAKE OFFSET HERE
            // This adds the shake offset to the normal camera position
            Camera.position += CarController.cameraShakeOffset;

            _xRotation -= Mouse_Y * MouseSensitivity * Time.smoothDeltaTime;
            _xRotation = Mathf.Clamp(_xRotation, UpperLimit, BottomLimit);

            Camera.localRotation = Quaternion.Euler(_xRotation, 0, 0);

            _playerRigidbody.MoveRotation(_playerRigidbody.rotation * Quaternion.Euler(0, Mouse_X * MouseSensitivity * Time.smoothDeltaTime, 0));
        }

        private void IntroCamPitchOnly()
        {
            if (!_hasAnimator) return;

            // Keep camera anchored with shake during intro while yaw is locked
            Camera.position = CameraRoot.position + CarController.cameraShakeOffset;

            var Mouse_Y = _inputManager.Look.y;
            _xRotation -= Mouse_Y * MouseSensitivity * Time.smoothDeltaTime;
            _xRotation = Mathf.Clamp(_xRotation, UpperLimit, BottomLimit);

            Camera.localRotation = Quaternion.Euler(_xRotation, 0, 0);
        }

        private void HandleJump()
        {
            if (!_hasAnimator) return;
            if (!_inputManager.Jump) return;
            if (!_grounded) return;
            _animator.SetTrigger(_jumpHash);
        }

        public void JumpAddForce()
        {
            _playerRigidbody.AddForce(-_playerRigidbody.linearVelocity.y * Vector3.up, ForceMode.VelocityChange);
            _playerRigidbody.AddForce(Vector3.up * JumpFactor, ForceMode.Impulse);
            _animator.ResetTrigger(_jumpHash);
        }

        private void SampleGround()
        {
            if (!_hasAnimator) return;

            RaycastHit hitInfo;
            if (Physics.Raycast(_playerRigidbody.worldCenterOfMass, Vector3.down, out hitInfo, Dis2Ground + 0.1f, GroundCheck))
            {
                _grounded = true;
                SetAnimationGrounding();
                return;
            }

            _grounded = false;
            _animator.SetFloat(_zVelHash, _playerRigidbody.linearVelocity.y);
            SetAnimationGrounding();
            return;
        }

        private void SetAnimationGrounding()
        {
            if (jumpEnabled)
            {
                // Keep animator grounded/falling parameters in sync with physics
                _animator.SetBool(_groundHash, _grounded);
                bool falling = !_grounded && _playerRigidbody.linearVelocity.y < -0.05f;
                _animator.SetBool(_fallingHash, falling);
            }
        }

        private void EnsureFootstepSource()
        {
            if (footstepSource != null) return;
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.spatialBlend = 1f;
        }

        private void HandleFootsteps()
        {
            if (!_grounded || _inputManager == null)
            {
                StopFootsteps();
                return;
            }

            float moveAmount = _inputManager.Move.magnitude;
            if (moveAmount < minSpeedForSteps)
            {
                StopFootsteps();
                return;
            }

            bool isRoadSurface = IsOnRoad();
            if (isRoadSurface)
            {
                PlayRoadLoop();
                return;
            }

            StopRoadLoopIfPlaying();

            float interval = walkStepInterval;

            _footstepTimer -= Time.fixedDeltaTime;
            if (_footstepTimer > 0f) return;
            _footstepTimer = interval;

            AudioClip clip = GetFootstepClip();
            if (clip == null || footstepSource == null) return;

            footstepSource.pitch = UnityEngine.Random.Range(footstepPitchMin, footstepPitchMax);
            footstepSource.volume = footstepVolume;
            footstepSource.PlayOneShot(clip);
        }

        private void StopFootsteps()
        {
            _footstepTimer = 0f;
            if (footstepSource != null && footstepSource.isPlaying)
                footstepSource.Stop();
        }

        private void PlayRoadLoop()
        {
            if (footstepSource == null || footstepRoadClip == null) return;

            if (!footstepSource.loop || footstepSource.clip != footstepRoadClip)
            {
                footstepSource.Stop();
                footstepSource.clip = footstepRoadClip;
                footstepSource.loop = true;
            }

            footstepSource.volume = footstepVolume;
            footstepSource.pitch = 1f;
            if (!footstepSource.isPlaying)
                footstepSource.Play();
        }

        private void StopRoadLoopIfPlaying()
        {
            if (footstepSource == null) return;
            if (!footstepSource.loop) return;

            footstepSource.Stop();
            footstepSource.loop = false;
            footstepSource.clip = null;
            _footstepTimer = 0f;
        }

        private bool IsOnRoad()
        {
            if (string.IsNullOrWhiteSpace(roadTag)) return false;

            RaycastHit hit;
            float rayDistance = Dis2Ground + 0.5f;
            Vector3 origin = _playerRigidbody != null ? _playerRigidbody.worldCenterOfMass : transform.position;
            if (Physics.Raycast(origin, Vector3.down, out hit, rayDistance, GroundCheck))
            {
                return hit.collider.CompareTag(roadTag);
            }

            return false;
        }

        private AudioClip GetFootstepClip()
        {
            RaycastHit hit;
            float rayDistance = Dis2Ground + 0.5f;
            Vector3 origin = _playerRigidbody != null ? _playerRigidbody.worldCenterOfMass : transform.position;
            if (Physics.Raycast(origin, Vector3.down, out hit, rayDistance, GroundCheck))
            {
                if (!string.IsNullOrWhiteSpace(sandTag) && hit.collider.CompareTag(sandTag)) return footstepSandClip;
                if (!string.IsNullOrWhiteSpace(roadTag) && hit.collider.CompareTag(roadTag)) return footstepRoadClip;
                if (!string.IsNullOrWhiteSpace(snowTag) && hit.collider.CompareTag(snowTag)) return footstepSnowClip;
            }

            return footstepRoadClip;
        }

        public bool IsMoving()
        {
            return new Vector2(_currentVelocity.x, _currentVelocity.y).magnitude > 0.05f;
        }
    }
}
