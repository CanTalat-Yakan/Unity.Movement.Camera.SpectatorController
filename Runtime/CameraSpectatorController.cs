using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEssentials
{
    [Serializable]
    public class CameraSpectatorControllerSettings
    {
        [Tooltip("Camera rotation by mouse movement is active")]
        public bool EnableRotation = true;
        [Tooltip("When enabled, hold Right Mouse Button to control rotation and translation")]
        public bool RequireRightMouseButton = true;
        [Tooltip("Sensitivity of mouse rotation")]
        public float MouseSense = 1.0f;
        [Tooltip("Maximum vertical look angle in degrees")]
        public float MaxVerticalAngle = 90f;

        [Space]
        [Tooltip("Camera zooming in/out by 'Mouse Scroll Wheel' is active")]
        public bool EnableTranslation = true;
        [Tooltip("Velocity of camera zooming in/out")]
        public float TranslationSpeed = 50f;

        [Space]
        [Tooltip("Camera movement by 'W','A','S','D','Q','E' keys is active")]
        public bool EnableMovement = true;
        [Tooltip("Camera movement speed")]
        public float MovementSpeed = 10f;
        [Tooltip("Speed of the quick camera movement when holding the 'Left Shift' key")]
        public float BoostedSpeed = 50f;
        public Key BoostSpeed = Key.LeftShift;
        public Key MoveForward = Key.W;
        public Key MoveBackward = Key.S;
        public Key MoveLeft = Key.A;
        public Key MoveRight = Key.D;
        public Key MoveUp = Key.E;
        public Key MoveDown = Key.Q;

        [Space]
        [Tooltip("Acceleration at camera movement is active")]
        public bool EnableSpeedAcceleration = true;
        [Tooltip("Rate which is applied during camera movement")]
        public float SpeedAccelerationFactor = 1.5f;

        [Space]
        [Tooltip("This keypress will move the camera to initialization position")]
        public Key InitPositionButton = Key.R;
    }

    public class CameraSpectatorController : MonoBehaviour
    {
        [Tooltip("The script is currently active")]
        [SerializeField] private bool _active = true;

        public CameraSpectatorControllerSettings Settings;

        [Space]
        private float _currentIncrease = 1;
        private float _currentIncreaseMem = 0;

        private Vector3 _initPosition;
        private Vector3 _initRotation;

        private bool _isCameraControlActive;
        private bool _isRightMouseHeld;

        private float _yaw;
        private float _pitch;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Settings.BoostedSpeed < Settings.MovementSpeed)
                Settings.BoostedSpeed = Settings.MovementSpeed;

            Settings.MaxVerticalAngle = Mathf.Clamp(Settings.MaxVerticalAngle, 0f, 90f);
        }
#endif

        public void Start()
        {
            _initPosition = transform.position;
            _initRotation = transform.eulerAngles;

            _yaw = transform.eulerAngles.y;
            _pitch = NormalizeAngle(transform.eulerAngles.x);
        }

        public void Update()
        {
            if (!_active)
            {
                _isRightMouseHeld = false;
                UpdateControlCursorState(false);
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard == null || mouse == null)
                return;

            if (Settings.RequireRightMouseButton)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                    _isRightMouseHeld = true;

                if (mouse.rightButton.wasReleasedThisFrame)
                    _isRightMouseHeld = false;
            }

            bool canControlCamera = !Settings.RequireRightMouseButton || _isRightMouseHeld;
            UpdateControlCursorState(canControlCamera);

            // Translation
            if (Settings.EnableTranslation && canControlCamera)
                transform.Translate(Vector3.forward * mouse.scroll.ReadValue().y * Time.deltaTime * Settings.TranslationSpeed);

            // Movement
            if (Settings.EnableMovement && canControlCamera)
            {
                Vector3 deltaPosition = Vector3.zero;
                float currentSpeed = Settings.MovementSpeed;

                if (keyboard[Settings.BoostSpeed].isPressed)
                    currentSpeed = Settings.BoostedSpeed;

                if (keyboard[Settings.MoveForward].isPressed)
                    deltaPosition += transform.forward;

                if (keyboard[Settings.MoveBackward].isPressed)
                    deltaPosition -= transform.forward;

                if (keyboard[Settings.MoveLeft].isPressed)
                    deltaPosition -= transform.right;

                if (keyboard[Settings.MoveRight].isPressed)
                    deltaPosition += transform.right;

                if (keyboard[Settings.MoveUp].isPressed)
                    deltaPosition += transform.up;

                if (keyboard[Settings.MoveDown].isPressed)
                    deltaPosition -= transform.up;

                // Calculate acceleration
                CalculateCurrentIncrease(deltaPosition != Vector3.zero);

                transform.position += deltaPosition * currentSpeed * _currentIncrease;
            }
            else
            {
                // Reset acceleration memory when control is not active.
                CalculateCurrentIncrease(false);
            }

            // Rotation
            if (Settings.EnableRotation && canControlCamera)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();

                _yaw += mouseDelta.x * Settings.MouseSense;
                _pitch = Mathf.Clamp(_pitch - mouseDelta.y * Settings.MouseSense, -Settings.MaxVerticalAngle, Settings.MaxVerticalAngle);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            // Return to initial position
            if (keyboard[Settings.InitPositionButton].wasPressedThisFrame)
            {
                transform.position = _initPosition;
                transform.eulerAngles = _initRotation;

                _yaw = _initRotation.y;
                _pitch = NormalizeAngle(_initRotation.x);
            }
        }

        private void OnDisable()
        {
            if (_isCameraControlActive)
                SetCursorState(CursorLockMode.None);

            _isCameraControlActive = false;
            _isRightMouseHeld = false;
        }

        private void UpdateControlCursorState(bool shouldControl)
        {
            if (shouldControl == _isCameraControlActive)
                return;

            _isCameraControlActive = shouldControl;
            SetCursorState(shouldControl ? CursorLockMode.Locked : CursorLockMode.None);
        }

       private void SetCursorState(CursorLockMode mode)
        {
            // Apply cursor state
            Cursor.lockState = mode;
            // Hide cursor when locking
            Cursor.visible = mode != CursorLockMode.Locked;
        }

        private void CalculateCurrentIncrease(bool moving)
        {
            _currentIncrease = Time.deltaTime;

            if (!Settings.EnableSpeedAcceleration || Settings.EnableSpeedAcceleration && !moving)
            {
                _currentIncreaseMem = 0;
                return;
            }

            _currentIncreaseMem += Time.deltaTime * (Settings.SpeedAccelerationFactor - 1);
            _currentIncrease = Time.deltaTime + Mathf.Pow(_currentIncreaseMem, 3) * Time.deltaTime;
        }

        private static float NormalizeAngle(float angle)
        {
            if (angle > 180f)
                angle -= 360f;

            return angle;
        }
    }
}