using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 15f;

    [Header("Camera Reference")]
    public Transform cameraTransform;

    [Header("Jump & Gravity")]
    public float gravity = -20f;
    public float jumpHeight = 1.2f;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundLayer;

    [Header("Animation")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // 1. Kiểm tra chạm đất
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer, QueryTriggerInteraction.Ignore);
        }
        else
        {
            isGrounded = controller.isGrounded;
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Nhận Input bàn phím (WASD)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (cameraTransform != null)
        {
            // Lấy hướng tiến/phải của camera (bỏ trục Y)
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 direction = camForward * vertical + camRight * horizontal;

            if (direction.magnitude >= 0.1f)
            {
                // Xoay nhân vật mượt mà theo hướng di chuyển
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Di chuyển
                isRunning = Input.GetKey(KeyCode.LeftShift);
                float currentSpeed = isRunning ? runSpeed : walkSpeed;
                controller.Move(direction * currentSpeed * Time.deltaTime);

                if (animator != null)
                    animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
            }
            else
            {
                if (animator != null)
                    animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            }
        }

        // 3. Nhảy
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 4. Trọng lực
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
        }
    }
}