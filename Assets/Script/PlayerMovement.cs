
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

    [Header("Interaction Animation")]
    public string shootTrigger = "Shoot";
    public string pickedTrigger = "Picked";

    [Header("Animation Speed")]
    [Range(0.5f, 3f)]
    public float pickedSpeed = 1.5f;

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

        if (animator != null)
        {
            animator.SetFloat("PickedSpeed", pickedSpeed);
        }
    }

    void Update()
    {
        // 1. Kiểm tra chạm đất
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
        }
        else
        {
            isGrounded = controller.isGrounded;
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Di chuyển WASD theo camera
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            Vector3 direction =
                camForward * vertical + camRight * horizontal;

            if (direction.magnitude >= 0.1f)
            {
                direction.Normalize();

                // Xoay theo hướng di chuyển
                Quaternion targetRotation =
                    Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );

                // Đi bộ hoặc chạy
                isRunning = Input.GetKey(KeyCode.LeftShift);

                float currentSpeed =
                    isRunning ? runSpeed : walkSpeed;

                controller.Move(
                    direction * currentSpeed * Time.deltaTime
                );

                if (animator != null)
                {
                    animator.SetFloat(
                        "Speed",
                        currentSpeed,
                        0.1f,
                        Time.deltaTime
                    );
                }
            }
            else
            {
                if (animator != null)
                {
                    animator.SetFloat(
                        "Speed",
                        0f,
                        0.1f,
                        Time.deltaTime
                    );
                }
            }
        }

        // 3. Nhảy
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(
                jumpHeight * -2f * gravity
            );
        }

        // 4. Trọng lực
        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // 5. Cập nhật Animator
        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);

            // Cập nhật tốc độ animation nhặt đồ
            animator.SetFloat("PickedSpeed", pickedSpeed);
        }

        // 6. Chuột trái: Shoot
        if (Input.GetMouseButtonDown(0))
        {
            RotateTowardsCamera();

            if (animator != null)
            {
                animator.ResetTrigger(pickedTrigger);
                animator.SetTrigger(shootTrigger);
            }
        }

        // 7. Phím E: Picked
        if (Input.GetKeyDown(KeyCode.E))
        {
            RotateTowardsCamera();

            if (animator != null)
            {
                animator.ResetTrigger(shootTrigger);
                animator.SetTrigger(pickedTrigger);
            }
        }
    }

    // Xoay nhân vật theo hướng camera
    private void RotateTowardsCamera()
    {
        if (cameraTransform == null) return;

        Vector3 lookDirection = cameraTransform.forward;

        // Bỏ trục Y để nhân vật không bị ngửa lên hoặc cúi xuống
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(lookDirection);
        }
    }
}
