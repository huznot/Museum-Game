using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

namespace UnityTutorial.Manager
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private PlayerInput PlayerInput;

        public Vector2 Move {get; private set;}
        public Vector2 Look {get; private set;}
        public bool Run {get; private set;}
        public bool Jump {get; private set;}
        public bool Crouch {get; private set;}

        private InputActionMap _currentMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _runAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;

        private void Awake() {
            HideCursor();
            if (PlayerInput == null)
                TryGetComponent(out PlayerInput);
            AssignCurrentMap();
        }

        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void onMove(InputAction.CallbackContext context)
        {
            Move = context.ReadValue<Vector2>();
        }
        private void onLook(InputAction.CallbackContext context)
        {
            Look = context.ReadValue<Vector2>();
        }
        private void onRun(InputAction.CallbackContext context)
        {
            Run = context.ReadValueAsButton();
        }
        private void onJump(InputAction.CallbackContext context)
        {
            Jump = context.ReadValueAsButton();
        }
        private void onCrouch(InputAction.CallbackContext context)
        {
            Crouch = context.ReadValueAsButton();
        }

        private void OnEnable() {
            AssignCurrentMap();
            if (_currentMap != null)
                _currentMap.Enable();
        }

        private void OnDisable() {
            if (_currentMap != null)
                _currentMap.Disable();
        }

        private void AssignCurrentMap()
        {
            if (PlayerInput == null)
                return;

            InputActionMap map = PlayerInput.currentActionMap;
            if (map == null && PlayerInput.actions != null)
            {
                if (!string.IsNullOrEmpty(PlayerInput.defaultActionMap))
                    map = PlayerInput.actions.FindActionMap(PlayerInput.defaultActionMap, false);

                if (map == null && PlayerInput.actions.actionMaps.Count > 0)
                    map = PlayerInput.actions.actionMaps[0];
            }

            if (map == null || map == _currentMap)
                return;

            UnbindActions();
            _currentMap = map;

            _moveAction = _currentMap.FindAction("Move", false);
            _lookAction = _currentMap.FindAction("Look", false);
            _runAction  = _currentMap.FindAction("Run", false);
            _jumpAction = _currentMap.FindAction("Jump", false);
            _crouchAction = _currentMap.FindAction("Crouch", false);

            BindActions();
        }

        private void BindActions()
        {
            if (_moveAction != null)
            {
                _moveAction.performed += onMove;
                _moveAction.canceled += onMove;
            }

            if (_lookAction != null)
            {
                _lookAction.performed += onLook;
                _lookAction.canceled += onLook;
            }

            if (_runAction != null)
            {
                _runAction.performed += onRun;
                _runAction.canceled += onRun;
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed += onJump;
                _jumpAction.canceled += onJump;
            }

            if (_crouchAction != null)
            {
                _crouchAction.started += onCrouch;
                _crouchAction.canceled += onCrouch;
            }
        }

        private void UnbindActions()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= onMove;
                _moveAction.canceled -= onMove;
            }

            if (_lookAction != null)
            {
                _lookAction.performed -= onLook;
                _lookAction.canceled -= onLook;
            }

            if (_runAction != null)
            {
                _runAction.performed -= onRun;
                _runAction.canceled -= onRun;
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed -= onJump;
                _jumpAction.canceled -= onJump;
            }

            if (_crouchAction != null)
            {
                _crouchAction.started -= onCrouch;
                _crouchAction.canceled -= onCrouch;
            }
        }
        
    }
}
