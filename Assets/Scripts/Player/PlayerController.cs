using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundCheckDistance = 0.4f;

        [Header("Camera Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float maxLookAngle = 80f;

        private CharacterController characterController;
        private Vector3 velocity;
        private bool isGrounded;
        private bool isRunning;
        
        private float xRotation = 0f;
        private Vector2 moveInput;
        private Vector2 lookInput;

        public bool IsMoving => moveInput.magnitude > 0.1f;
        public bool IsRunning => isRunning;
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            
            if (cameraTransform == null)
            {
                cameraTransform = UnityEngine.Camera.main?.transform;
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleInput();
            HandleMovement();
            HandleCamera();
        }

        private void HandleInput()
        {
            if (InputManager.Instance != null)
            {
                Vector2 movement = InputManager.Instance.GetMovementInputRaw();
                moveInput.x = movement.x;
                moveInput.y = movement.y;
                
                isRunning = InputManager.Instance.IsRunPressed();

                if (InputManager.Instance.IsDodgePressed() && isGrounded)
                {
                    Jump();
                }

                if (InputManager.Instance.IsMenuPressed())
                {
                    ToggleCursor();
                }
            }
            else
            {
                // Fallback to direct input
                moveInput.x = Input.GetAxisRaw("Horizontal");
                moveInput.y = Input.GetAxisRaw("Vertical");
                isRunning = Input.GetKey(KeyCode.LeftShift);

                if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
                {
                    Jump();
                }

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ToggleCursor();
                }
            }
            
            // Mouse look (remains direct for camera control)
            lookInput.x = Input.GetAxis("Mouse X");
            lookInput.y = Input.GetAxis("Mouse Y");
        }

        private void HandleMovement()
        {
            isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, LayerMask.GetMask("Ground"));

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            float currentSpeed = isRunning ? runSpeed : moveSpeed;
            
            characterController.Move(move * currentSpeed * Time.deltaTime);

            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleCamera()
        {
            if (cameraTransform == null) return;

            lookInput *= mouseSensitivity;

            xRotation -= lookInput.y;
            xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * lookInput.x);
        }

        private void Jump()
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        private void ToggleCursor()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void SetMovementEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

        public Vector3 GetVelocity()
        {
            return characterController.velocity;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, groundCheckDistance);
        }
    }
}