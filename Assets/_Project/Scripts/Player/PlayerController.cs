using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTutorial.Manager;

namespace UnityTutorial.PlayerControl
{
    public class PlayerController : MonoBehaviour
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
        [SerializeField] private AudioSource roadLoopSource;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.85f;
        [SerializeField] private float footstepPitchMin = 0.95f;
        [SerializeField] private float footstepPitchMax = 1.05f;
        [SerializeField] private float minSpeedForSteps = 0.1f;
        [SerializeField] private float walkStepInterval = 0.5f;
        [SerializeField] private float runStepInterval = 0.35f;
        [SerializeField] private float crouchStepInterval = 0.7f;
        [SerializeField] private string sandTag = "Sand";
        [SerializeField] private string roadTag = "Road";
        [SerializeField] private string snowTag = "Snow";
        public bool jumpEnabled = false;
        public bool movementDisabled = false;
        public bool uiMode = false;
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
        private int _crouchHash;
        private float _xRotation;
        private float _footstepTimer;

        private const float _walkSpeed = 3f;
        private const float _runSpeed = 6f;
        private Vector2 _currentVelocity;

        public float GroundCheckDistance => Dis2Ground;
        public LayerMask GroundCheckMask => GroundCheck;


        private void Start()
        {
            _hasAnimator = TryGetComponent<Animator>(out _animator);
            _playerRigidbody = GetComponent<Rigidbody>();
            _inputManager = GetComponent<InputManager>();

            _xVelHash = Animator.StringToHash("X_Velocity");
            _yVelHash = Animator.StringToHash("Y_Velocity");
            _zVelHash = Animator.StringToHash("Z_Velocity");
            _jumpHash = Animator.StringToHash("Jump");
            _groundHash = Animator.StringToHash("Grounded");
            _fallingHash = Animator.StringToHash("Falling");
            _crouchHash = Animator.StringToHash("Crouch");

            EnsureFootstepSource();
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
                HandleCrouch();
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
                CamMovements();
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

            float targetSpeed = _inputManager.Run ? _runSpeed : _walkSpeed;
            if (_inputManager.Crouch) targetSpeed = 1.5f;
            if (_inputManager.Move == Vector2.zero) targetSpeed = 0;

            if (_grounded)
            {

                _currentVelocity.x = Mathf.Lerp(_currentVelocity.x, _inputManager.Move.x * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);
                _currentVelocity.y = Mathf.Lerp(_currentVelocity.y, _inputManager.Move.y * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);

                var xVelDifference = _currentVelocity.x - _playerRigidbody.linearVelocity.x;
                var zVelDifference = _currentVelocity.y - _playerRigidbody.linearVelocity.z;


                // Apply a safety clamp to prevent extreme forces
                float maxSafeDiff = 10f;
                xVelDifference = Mathf.Clamp(xVelDifference, -maxSafeDiff, maxSafeDiff);
                zVelDifference = Mathf.Clamp(zVelDifference, -maxSafeDiff, maxSafeDiff);

                _playerRigidbody.AddForce(transform.TransformVector(new Vector3(xVelDifference, 0, zVelDifference)), ForceMode.VelocityChange);
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

        private void HandleCrouch() => _animator.SetBool(_crouchHash, _inputManager.Crouch);

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
            if (_inputManager.Crouch) interval = crouchStepInterval;
            else if (_inputManager.Run) interval = runStepInterval;

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
            if (footstepRoadClip == null) return;

            EnsureRoadLoopSource();
            StopOneShotIfPlaying();
            StopOtherRoadSources();

            if (roadLoopSource.clip != footstepRoadClip)
            {
                roadLoopSource.Stop();
                roadLoopSource.clip = footstepRoadClip;
            }

            roadLoopSource.volume = footstepVolume;
            roadLoopSource.pitch = 1f;
            if (!roadLoopSource.isPlaying)
                roadLoopSource.Play();
        }

        private void StopRoadLoopIfPlaying()
        {
            if (roadLoopSource == null) return;
            if (!roadLoopSource.isPlaying) return;

            roadLoopSource.Stop();
            roadLoopSource.clip = null;
            _footstepTimer = 0f;
        }

        private void EnsureRoadLoopSource()
        {
            if (roadLoopSource != null) return;
            roadLoopSource = gameObject.AddComponent<AudioSource>();
            roadLoopSource.playOnAwake = false;
            roadLoopSource.loop = true;
            roadLoopSource.spatialBlend = 1f;
        }

        private void StopOneShotIfPlaying()
        {
            if (footstepSource == null) return;
            if (!footstepSource.isPlaying) return;
            footstepSource.Stop();
            footstepSource.clip = null;
        }

        private void StopOtherRoadSources()
        {
            var sources = GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                var src = sources[i];
                if (src == null) continue;
                if (src == roadLoopSource) continue;
                if (!src.isPlaying) continue;
                if (src.clip != footstepRoadClip) continue;
                src.Stop();
            }
        }

        private bool IsOnRoad()
        {
            if (string.IsNullOrWhiteSpace(roadTag)) return false;

            RaycastHit hit;
            float rayDistance = Dis2Ground + 0.5f;
            Vector3 origin = _playerRigidbody != null ? _playerRigidbody.worldCenterOfMass : transform.position;
            int mask = GroundCheck.value == 0 ? Physics.AllLayers : GroundCheck.value;
            if (Physics.Raycast(origin, Vector3.down, out hit, rayDistance, mask))
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
            int mask = GroundCheck.value == 0 ? Physics.AllLayers : GroundCheck.value;
            if (Physics.Raycast(origin, Vector3.down, out hit, rayDistance, mask))
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
